using System.Text.Json;
using System.Text.RegularExpressions;
using Documenter.ConfluencePipeline.Config;
using Microsoft.Extensions.Logging;

namespace ConfluencePipeline.Stages;

public partial class MarkdownCategorizer(CategoryConfig config, ILogger<MarkdownCategorizer> logger)
{
    [GeneratedRegex(@"^title:\s*[""']?(.+?)[""']?\s*$", RegexOptions.Multiline)]
    private static partial Regex TitleFmRegex();
    [GeneratedRegex(@"^#\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex TitleBodyRegex();

    private static readonly Dictionary<string, CategoryRule> Rules = new()
    {
        ["api"] = new(
            ["endpoint","rest","graphql","http","request","response","payload",
             "authentication","openapi","swagger","curl","status code","bearer",
             "oauth","rate limit","api key"],
            [new Regex(@"api", RegexOptions.IgnoreCase),
             new Regex(@"endpoint", RegexOptions.IgnoreCase),
             new Regex(@"service", RegexOptions.IgnoreCase)],
            1.0),
        ["architecture"] = new(
            ["architecture","diagram","component","system design","microservice",
             "event","kafka","queue","database","infrastructure","cloud","azure",
             "aws","deployment"],
            [new Regex(@"arch", RegexOptions.IgnoreCase),
             new Regex(@"design", RegexOptions.IgnoreCase),
             new Regex(@"infra", RegexOptions.IgnoreCase)],
            1.0),
        ["runbook"] = new(
            ["runbook","incident","on-call","alert","escalation","troubleshoot",
             "restart","rollback","monitoring","steps to","procedure","checklist","sop"],
            [new Regex(@"runbook", RegexOptions.IgnoreCase),
             new Regex(@"incident", RegexOptions.IgnoreCase),
             new Regex(@"procedure", RegexOptions.IgnoreCase)],
            1.1),
        ["adr"] = new(
            ["decision record","adr","status: accepted","status: proposed",
             "consequences","context and problem","considered options"],
            [new Regex(@"adr[-\s]?\d+", RegexOptions.IgnoreCase),
             new Regex(@"decision[-\s]record", RegexOptions.IgnoreCase)],
            1.2),
        ["glossary"] = new(
            ["glossary","definition","terminology","term:","refers to",
             "is defined as","abbreviation"],
            [new Regex(@"glossary", RegexOptions.IgnoreCase),
             new Regex(@"terms", RegexOptions.IgnoreCase)],
            1.1),
    };

    // ── Entry point ────────────────────────────────────────────────────

    public async Task<List<CategoryResult>> CategorizeAllAsync(CancellationToken ct = default)
    {
        var results = new List<CategoryResult>();

        foreach (var mdFile in Directory.EnumerateFiles(config.InputDir, "**/*.md",
                     SearchOption.AllDirectories)
                     .Where(f => !Path.GetFileName(f).StartsWith('_')))
        {
            ct.ThrowIfCancellationRequested();
            var result = await CategorizeFileAsync(mdFile);
            results.Add(result);
            logger.LogDebug("{File} → {Cat} ({Conf:P0})",
                Path.GetFileName(mdFile), result.Category, result.Confidence);
        }

        if (config.GenerateIndex) await WriteIndexAsync(results);

        var summary = results.GroupBy(r => r.Category)
                             .ToDictionary(g => g.Key, g => g.Count());
        logger.LogInformation("Categorized {Total} files: {Summary}",
            results.Count, string.Join(", ", summary.Select(kv => $"{kv.Key}:{kv.Value}")));
        return results;
    }

    // ── Per-file ───────────────────────────────────────────────────────

    private async Task<CategoryResult> CategorizeFileAsync(string mdFile)
    {
        var text = await File.ReadAllTextAsync(mdFile);
        var (frontMatter, body) = SplitFrontMatter(text);
        var title = ExtractTitle(frontMatter, body, Path.GetFileNameWithoutExtension(mdFile));

        var (category, confidence) = Score(title, body);

        var catDir = Path.Combine(config.OutputDir, category);
        Directory.CreateDirectory(catDir);
        var dest = Path.Combine(catDir, Path.GetFileName(mdFile));

        if (config.MetadataOutput == "front-matter")
        {
            var updatedFm = InjectCategory(frontMatter, category, confidence);
            await File.WriteAllTextAsync(dest, updatedFm + body);
        }
        else
        {
            File.Copy(mdFile, dest, overwrite: true);
            var sidecar = Path.ChangeExtension(dest, ".meta.json");
            await File.WriteAllTextAsync(sidecar, JsonSerializer.Serialize(new
            {
                file = Path.GetFileName(mdFile), category, confidence = Math.Round(confidence, 3), title
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new CategoryResult(mdFile, dest, title, category, confidence);
    }

    // ── Scoring ────────────────────────────────────────────────────────

    private (string Category, double Confidence) Score(string title, string body)
    {
        var combined = (title + " " + body).ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();
        var scores = new Dictionary<string, double>();

        foreach (var (cat, rule) in Rules)
        {
            double score = 0;
            score += rule.Keywords.Count(kw => combined.Contains(kw, StringComparison.Ordinal)) * 0.05;
            score += rule.TitlePatterns.Count(p => p.IsMatch(titleLower)) * 0.35;
            scores[cat] = score * rule.Weight;
        }

        if (scores.Values.All(v => v == 0)) return ("uncategorized", 0.0);

        var best  = scores.MaxBy(kv => kv.Value).Key;
        var total = scores.Values.Sum();
        var conf  = total > 0 ? scores[best] / total : 0.0;

        if (conf < config.MinConfidence) return ("uncategorized", conf);
        return (best, conf);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static (string Fm, string Body) SplitFrontMatter(string text)
    {
        if (!text.StartsWith("---")) return ("", text);
        var end = text.IndexOf("---", 3, StringComparison.Ordinal);
        if (end < 0) return ("", text);
        return (text[..(end + 3)] + "\n", text[(end + 3)..]);
    }

    private static string ExtractTitle(string fm, string body, string fallback)
    {
        var m = TitleFmRegex().Match(fm);
        if (m.Success) return m.Groups[1].Value;
        m = TitleBodyRegex().Match(body);
        if (m.Success) return m.Groups[1].Value;
        return fallback;
    }

    private static string InjectCategory(string fm, string cat, double conf) =>
        fm.EndsWith("---\n")
            ? fm[..^4] + $"category: \"{cat}\"\ncategory_confidence: {Math.Round(conf, 3)}\n---\n"
            : fm + $"category: \"{cat}\"\ncategory_confidence: {Math.Round(conf, 3)}\n";

    private async Task WriteIndexAsync(List<CategoryResult> results)
    {
        var index = results.GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Select(r => new
            {
                title      = r.Title,
                file       = Path.GetFileName(r.Dest),
                confidence = Math.Round(r.Confidence, 3),
            }).ToList());

        var path = Path.Combine(config.OutputDir, "_category_index.json");
        await File.WriteAllTextAsync(path,
            JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
        logger.LogInformation("Category index → {Path}", path);
    }
}

internal record CategoryRule(
    List<string>  Keywords,
    List<Regex>   TitlePatterns,
    double        Weight
);