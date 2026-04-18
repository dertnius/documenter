using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Documenter.ConfluencePipeline.Config;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfluencePipeline.Stages;

public partial class OasGenerator(OpenApiConfig config, ILogger<OasGenerator> logger)
{
    [GeneratedRegex(@"(?:#{1,4}\s+)?`?(GET|POST|PUT|PATCH|DELETE)\s+(/[^\s`]+)`?",
        RegexOptions.IgnoreCase)]
    private static partial Regex EndpointRegex();

    [GeneratedRegex(@"\|([^|]+)\|([^|]+)\|([^|]+)\|")]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex PathParamRegex();

    [GeneratedRegex(@"#{1,4}\s+(.+)")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"service[:\s]+([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ServiceNameRegex();

    private static readonly ISerializer YamlSerializer =
        new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

    // ── Entry point ────────────────────────────────────────────────────

    public async Task<List<string>> GenerateAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(config.OutputDir);
        var mdFiles = Directory.EnumerateFiles(config.InputDir, "*.md",
                          SearchOption.AllDirectories).ToList();

        if (mdFiles.Count == 0)
        {
            logger.LogWarning("No markdown files found in {Dir}", config.InputDir);
            return [];
        }

        if (config.SplitMode == "monolith")
            return [await GenerateSpecAsync(config.InfoTitle, mdFiles, ct)];

        // per-service grouping
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in mdFiles)
        {
            var svc = DetectService(f);
            if (!grouped.TryGetValue(svc, out var list))
                grouped[svc] = list = [];
            list.Add(f);
        }

        var specs = new List<string>();
        foreach (var (svc, files) in grouped)
            specs.Add(await GenerateSpecAsync(svc, files, ct));

        logger.LogInformation("Generated {Count} OAS specs → {Dir}", specs.Count, config.OutputDir);
        return specs;
    }

    // ── Spec builder ───────────────────────────────────────────────────

    private async Task<string> GenerateSpecAsync(
        string service, List<string> files, CancellationToken ct)
    {
        var endpoints = new List<ParsedEndpoint>();
        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();
            endpoints.AddRange(ExtractEndpoints(await File.ReadAllTextAsync(f, ct)));
        }

        var spec = BuildOas(service, endpoints);
        var safeName = Regex.Replace(service, @"[^\w\-]", "_").ToLower();
        var path = Path.Combine(config.OutputDir, $"{safeName}.yaml");
        await File.WriteAllTextAsync(path, YamlSerializer.Serialize(spec), ct);
        logger.LogInformation("  {File} ({Count} endpoints)", Path.GetFileName(path), endpoints.Count);
        return path;
    }

    private Dictionary<string, object> BuildOas(
        string service, List<ParsedEndpoint> endpoints)
    {
        var paths = new Dictionary<string, object>();
        foreach (var ep in endpoints)
        {
            if (!paths.TryGetValue(ep.Path, out var pathItem))
                paths[ep.Path] = pathItem = new Dictionary<string, object>();

            var method = new Dictionary<string, object>
            {
                ["summary"]  = ep.Summary.Length > 0 ? ep.Summary : $"{ep.Method} {ep.Path}",
                ["tags"]     = new[] { service },
                ["responses"] = new Dictionary<string, object>
                {
                    ["200"] = new { description = "Success" },
                    ["400"] = new { description = "Bad request" },
                    ["401"] = new { description = "Unauthorized" },
                    ["500"] = new { description = "Internal server error" },
                },
            };

            if (ep.Description.Length > 0)    method["description"] = ep.Description;
            if (ep.Parameters.Count > 0)      method["parameters"]  = ep.Parameters;
            if (ep.IsInternal && config.MarkInternal) method["x-internal"] = true;
            if (ep.RequestSchema is not null &&
                ep.Method is "POST" or "PUT" or "PATCH")
            {
                method["requestBody"] = new
                {
                    required = true,
                    content  = new Dictionary<string, object>
                    {
                        ["application/json"] = new { schema = ep.RequestSchema }
                    }
                };
            }

            ((Dictionary<string, object>)pathItem)[ep.Method.ToLower()] = method;
        }

        return new Dictionary<string, object>
        {
            ["openapi"] = config.OasVersion,
            ["info"]    = new { title = service, version = config.InfoVersion },
            ["paths"]   = paths,
            ["components"] = new
            {
                securitySchemes = new
                {
                    BearerAuth = new { type = "http", scheme = "bearer", bearerFormat = "JWT" }
                }
            },
            ["security"] = new[] { new Dictionary<string, string[]> { ["BearerAuth"] = [] } },
        };
    }

    // ── Endpoint extraction ────────────────────────────────────────────

    private List<ParsedEndpoint> ExtractEndpoints(string text)
    {
        var lines   = text.Split('\n');
        var result  = new List<ParsedEndpoint>();

        for (int i = 0; i < lines.Length; i++)
        {
            var m = EndpointRegex().Match(lines[i]);
            if (!m.Success) continue;

            var method = m.Groups[1].Value.ToUpper();
            var path   = m.Groups[2].Value;

            // Summary from heading above
            var summary = "";
            for (int j = i - 1; j >= Math.Max(0, i - 5); j--)
            {
                var hm = HeadingRegex().Match(lines[j]);
                if (hm.Success) { summary = hm.Groups[1].Value.Trim(); break; }
            }

            // Description from text below
            var descParts = new List<string>();
            for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
            {
                var l = lines[j].Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith('#') || EndpointRegex().IsMatch(l)) break;
                if (!l.StartsWith('|')) descParts.Add(l);
            }

            // Path params
            var pathParams = PathParamRegex().Matches(path)
                .Select(pm => new OasParameter(pm.Groups[1].Value, "path", true, "string"))
                .ToList();

            // Schema from table
            Dictionary<string, object>? schema = null;
            if (config.SchemaInference is "tables" or "both")
                schema = ExtractTableSchema(lines, i);

            var context = string.Join(" ", lines[Math.Max(0,i-5)..Math.Min(lines.Length,i+10)]).ToLower();
            var isInternal = context.Contains("internal");

            result.Add(new ParsedEndpoint(method, path, summary,
                string.Join(" ", descParts), pathParams, schema, isInternal));
        }
        return result;
    }

    private static Dictionary<string, object>? ExtractTableSchema(string[] lines, int anchor)
    {
        var props    = new Dictionary<string, object>();
        var required = new List<string>();

        for (int i = anchor; i < Math.Min(anchor + 20, lines.Length); i++)
        {
            var m = TableRowRegex().Match(lines[i].Trim());
            if (!m.Success) continue;
            var (c1, c2, c3) = (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim(), m.Groups[3].Value.Trim());
            if (c1.ToLower() is "field" or "parameter" or "name" or "property") continue;
            if (c1.StartsWith('-')) continue;
            var fieldName = c1.Trim('`', '*', ' ');
            if (string.IsNullOrEmpty(fieldName)) continue;
            props[fieldName] = new { type = InferOasType(c2), description = c3 };
            if (c3.Contains("required", StringComparison.OrdinalIgnoreCase))
                required.Add(fieldName);
        }

        if (props.Count == 0) return null;
        var schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = props };
        if (required.Count > 0) schema["required"] = required;
        return schema;
    }

    private static string InferOasType(string raw)
    {
        var lower = raw.ToLower().Trim();
        if (lower.ContainsAny(["int","long","number","float","double"])) return "number";
        if (lower.Contains("bool"))                                       return "boolean";
        if (lower.ContainsAny(["array","list","[]"]))                    return "array";
        if (lower.ContainsAny(["object","dict","map","{}"]))             return "object";
        return "string";
    }

    private static string DetectService(string filePath)
    {
        var text = File.ReadAllText(filePath, Encoding.UTF8)[..Math.Min(2000, (int)new FileInfo(filePath).Length)];
        var m = ServiceNameRegex().Match(text);
        if (m.Success) return m.Groups[1].Value.ToLower();
        m = Regex.Match(text, @"^#\s+(.+)", RegexOptions.Multiline);
        if (m.Success) return Regex.Replace(m.Groups[1].Value.ToLower(), @"[^\w]", "-")[..Math.Min(40, m.Groups[1].Length)];
        return "api";
    }
}

internal static class StringExtensions
{
    public static bool ContainsAny(this string s, IEnumerable<string> values) =>
        values.Any(v => s.Contains(v, StringComparison.Ordinal));
}