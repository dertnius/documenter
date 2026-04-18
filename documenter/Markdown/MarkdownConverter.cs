using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Documenter.ConfluencePipeline.Config;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace ConfluencePipeline.Stages;

public partial class MarkdownConverter(MarkdownConfig config, ILogger<MarkdownConverter> logger)
{
    private readonly Dictionary<string, string> _idToTitle    = new();
    private readonly Dictionary<string, string> _idToFilename = new();

    [GeneratedRegex(@"[^\w\-]")]
    private static partial Regex UnsafeCharsRegex();

    // ── Entry point ────────────────────────────────────────────────────

    public async Task<List<string>> ConvertAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(config.OutputDir);
        Directory.CreateDirectory(config.AssetsDir);
        BuildIndex();

        var converted = new List<string>();
        foreach (var jsonFile in Directory.EnumerateFiles(config.InputDir, "*.json")
                                          .Where(f => !f.EndsWith("_index.json")))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var dest = await ConvertFileAsync(jsonFile);
                converted.Add(dest);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Skipping {File}: {Msg}", Path.GetFileName(jsonFile), ex.Message);
            }
        }

        logger.LogInformation("Converted {Count} files → {Dir}", converted.Count, config.OutputDir);
        return converted;
    }

    // ── Index ──────────────────────────────────────────────────────────

    private void BuildIndex()
    {
        var indexPath = Path.Combine(config.InputDir, "_index.json");
        if (!File.Exists(indexPath)) return;

        var idx = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                      File.ReadAllText(indexPath)) ?? new();
        foreach (var (pid, meta) in idx)
        {
            var title = meta.GetProperty("Title").GetString() ?? "";
            var safe  = SafeFilename(title);
            _idToTitle[pid]    = title;
            _idToFilename[pid] = $"{pid}_{safe[..Math.Min(60, safe.Length)]}.md";
        }
    }

    // ── Per-file ───────────────────────────────────────────────────────
    
    private async Task<string> ConvertFileAsync(string jsonFile)
    {
        var data  = JsonSerializer.Deserialize<PageData>(await File.ReadAllTextAsync(jsonFile))!;
        var safe  = SafeFilename(data.Title);
        var dest  = Path.Combine(config.OutputDir, $"{data.Id}_{safe[..Math.Min(60, safe.Length)]}.md");

        var sb = new StringBuilder();
        sb.Append(BuildFrontMatter(data));

        if (config.AddBreadcrumb && data.Breadcrumb.Count > 0)
        {
            var crumbs = string.Join(" › ", data.Breadcrumb.Append(data.Title));
            sb.AppendLine($"<!-- breadcrumb: {crumbs} -->");
            sb.AppendLine();
        }

        sb.Append(XhtmlToMarkdown(data.BodyStorage, data));

        await File.WriteAllTextAsync(dest, sb.ToString());
        logger.LogDebug("  → {File}", Path.GetFileName(dest));
        return dest;
    }

    // ── Front-matter ───────────────────────────────────────────────────

    private static string BuildFrontMatter(PageData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{d.Title.Replace("\"", "'")}\"");
        sb.AppendLine($"confluence_id: \"{d.Id}\"");
        sb.AppendLine($"space: \"{d.SpaceKey}\"");
        sb.AppendLine($"author: \"{d.Author}\"");
        sb.AppendLine($"created: \"{(d.Created.Length >= 10 ? d.Created[..10] : d.Created)}\"");
        sb.AppendLine($"updated: \"{(d.Updated.Length >= 10 ? d.Updated[..10] : d.Updated)}\"");
        sb.AppendLine($"parent: \"{d.ParentTitle ?? ""}\"");
        if (d.Labels.Count > 0)
        {
            sb.AppendLine("labels:");
            foreach (var l in d.Labels) sb.AppendLine($"  - {l}");
        }
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {d.Title}");
        sb.AppendLine();
        return sb.ToString();
    }

    // ── XHTML → Markdown ───────────────────────────────────────────────

    private string XhtmlToMarkdown(string xhtml, PageData data)
    {
        if (string.IsNullOrWhiteSpace(xhtml)) return "_No content._\n";

        var doc = new HtmlDocument();
        doc.LoadHtml(xhtml);
        var root = doc.DocumentNode;

        HandleGliffy(root, data);
        HandleMacros(root);
        HandleLinks(root);

        return NodeToMarkdown(root).Trim() + "\n";
    }

    private void HandleGliffy(HtmlNode root, PageData data)
    {
        var attachments = data.Attachments.ToDictionary(a => a.Filename, a => a);

        foreach (var macro in root.SelectNodes(
            "//ac:structured-macro[@ac:name='gliffy']")?.ToList() ?? [])
        {
            var nameParam = macro.SelectSingleNode(".//ac:parameter[@ac:name='name']");
            var fname = nameParam?.InnerText.Trim();

            if (fname != null && attachments.TryGetValue(fname, out var att)
                && config.GliffyMode != "png")
            {
                var bytes = Convert.FromBase64String(att.ContentBase64);
                var svgContent = GliffyToSvg(bytes, fname);

                if (config.GliffyMode == "svg-inline")
                {
                    macro.ParentNode.ReplaceChild(
                        HtmlNode.CreateNode($"\n\n{svgContent}\n\n"), macro);
                }
                else
                {
                    var svgName = fname.Replace(".gliffy", ".svg", StringComparison.OrdinalIgnoreCase);
                    File.WriteAllText(Path.Combine(config.AssetsDir, svgName), svgContent);
                    var alt = Path.GetFileNameWithoutExtension(fname).Replace("-", " ");
                    macro.ParentNode.ReplaceChild(
                        HtmlNode.CreateNode($"\n\n![{alt}](assets/{svgName})\n\n"), macro);
                }
            }
            else
            {
                var alt = fname ?? "diagram";
                macro.ParentNode.ReplaceChild(
                    HtmlNode.CreateNode($"\n\n> **[Diagram: {alt}]**\n\n"), macro);
            }
        }
    }

    private static string GliffyToSvg(byte[] jsonBytes, string fname)
    {
        // Minimal placeholder — extend with full shape rendering as needed
        try
        {
            var gliffyData = JsonSerializer.Deserialize<JsonElement>(jsonBytes);
            var stage = gliffyData.GetProperty("stage");
            var w = stage.TryGetProperty("width",  out var we) ? we.GetInt32() : 800;
            var h = stage.TryGetProperty("height", out var he) ? he.GetInt32() : 600;
            var label = Path.GetFileNameWithoutExtension(fname);
            return $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">
                  <rect width="{w}" height="{h}" fill="#f5f5f5" stroke="#ccc"/>
                  <text x="{w/2}" y="{h/2}" text-anchor="middle" font-family="sans-serif" font-size="14" fill="#666">{label}</text>
                </svg>
                """;
        }
        catch
        {
            return $"<!-- gliffy-svg-error: {fname} -->";
        }
    }

    private static readonly HashSet<string> UnsupportedMacros =
        ["recently-updated", "space-index", "children", "navmap", "pagetree", "search"];

    private static void HandleMacros(HtmlNode root)
    {
        foreach (var macro in root.SelectNodes("//ac:structured-macro")?.ToList() ?? [])
        {
            var name = macro.GetAttributeValue("ac:name", "");
            if (UnsupportedMacros.Contains(name))
            {
                macro.ParentNode.ReplaceChild(
                    HtmlNode.CreateNode($"<!-- confluence-macro:{name} removed -->"), macro);
            }
            else if (name == "code")
            {
                var lang = macro.SelectSingleNode(".//ac:parameter[@ac:name='language']")?.InnerText ?? "";
                var body = macro.SelectSingleNode(".//ac:plain-text-body")?.InnerText ?? "";
                macro.ParentNode.ReplaceChild(
                    HtmlNode.CreateNode($"\n\n```{lang}\n{body}\n```\n\n"), macro);
            }
            else if (name == "info")
            {
                var text = macro.SelectSingleNode(".//ac:rich-text-body")?.InnerText.Trim() ?? "";
                macro.ParentNode.ReplaceChild(
                    HtmlNode.CreateNode($"\n\n> **ℹ Info:** {text}\n\n"), macro);
            }
            else if (name == "warning")
            {
                var text = macro.SelectSingleNode(".//ac:rich-text-body")?.InnerText.Trim() ?? "";
                macro.ParentNode.ReplaceChild(
                    HtmlNode.CreateNode($"\n\n> ⚠️ **Warning:** {text}\n\n"), macro);
            }
        }
    }

    private void HandleLinks(HtmlNode root)
    {
        foreach (var link in root.SelectNodes("//ac:link")?.ToList() ?? [])
        {
            var pageRef   = link.SelectSingleNode(".//ri:page");
            var pageTitle = pageRef?.GetAttributeValue("ri:content-title", "");
            var labelNode = link.SelectSingleNode(".//ac:link-body");
            var text      = labelNode?.InnerText ?? pageTitle ?? "";

            var targetFile = _idToFilename.FirstOrDefault(
                kvp => _idToTitle.TryGetValue(kvp.Key, out var t) && t == pageTitle).Value;

            link.ParentNode.ReplaceChild(
                HtmlNode.CreateNode(targetFile != null
                    ? $"[{text}]({targetFile})"
                    : $"[{text}]"), link);
        }
    }

    // ── Node renderer ──────────────────────────────────────────────────

    private static string NodeToMarkdown(HtmlNode node)
    {
        var sb = new StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                sb.Append(child.InnerText);
                continue;
            }
            switch (child.Name.ToLower())
            {
                case "h1": case "h2": case "h3":
                case "h4": case "h5": case "h6":
                    int lvl = int.Parse(child.Name[1..]);
                    sb.AppendLine().AppendLine($"{new string('#', lvl)} {child.InnerText}").AppendLine();
                    break;
                case "p":  sb.AppendLine().Append(NodeToMarkdown(child)).AppendLine(); break;
                case "strong": case "b": sb.Append($"**{child.InnerText}**"); break;
                case "em":     case "i": sb.Append($"_{child.InnerText}_");   break;
                case "code": sb.Append($"`{child.InnerText}`"); break;
                case "a":
                    sb.Append($"[{child.InnerText}]({child.GetAttributeValue("href","")})");
                    break;
                case "ul":
                    foreach (var li in child.SelectNodes("li") ?? Enumerable.Empty<HtmlNode>())
                        sb.AppendLine($"\n- {NodeToMarkdown(li).Trim()}");
                    sb.AppendLine();
                    break;
                case "ol":
                    int n = 1;
                    foreach (var li in child.SelectNodes("li") ?? Enumerable.Empty<HtmlNode>())
                        sb.AppendLine($"\n{n++}. {NodeToMarkdown(li).Trim()}");
                    sb.AppendLine();
                    break;
                case "table": sb.Append(TableToMarkdown(child)); break;
                case "br":    sb.Append("  \n"); break;
                case "hr":    sb.AppendLine().AppendLine("---").AppendLine(); break;
                default:      sb.Append(NodeToMarkdown(child)); break;
            }
        }
        return sb.ToString();
    }

    private static string TableToMarkdown(HtmlNode table)
    {
        var rows = table.SelectNodes(".//tr")?.ToList() ?? [];
        if (rows.Count == 0) return "";
        var sb = new StringBuilder("\n\n");
        for (int i = 0; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes("th|td")?
                .Select(c => c.InnerText.Trim()) ?? [];
            var cols = cells.ToList();
            sb.AppendLine("| " + string.Join(" | ", cols) + " |");
            if (i == 0)
                sb.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", cols.Count)) + " |");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static string SafeFilename(string s) =>
        UnsafeCharsRegex().Replace(s, "_").Trim('_');
}