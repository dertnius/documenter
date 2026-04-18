using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Documenter.ConfluencePipeline.Config;
using Microsoft.Extensions.Logging;

namespace Documenter.ConfluencePipeline;

public class ConfluenceExporter(ConfluenceConfig config, ILogger<ConfluenceExporter> logger)
{
    private readonly HttpClient _http = BuildHttpClient(config);

    private static HttpClient BuildHttpClient(ConfluenceConfig cfg)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = cfg.AuthType == "bearer"
            ? new AuthenticationHeaderValue("Bearer", cfg.AuthToken)
            : new AuthenticationHeaderValue("Basic",  cfg.AuthToken);
        return client;
    }

    // ── Entry point ────────────────────────────────────────────────────

    public async Task<List<PageData>> ExportSpacesAsync(CancellationToken ct = default)
    {
        var all = new List<PageData>();
        foreach (var space in config.SpaceKeys)
        {
            logger.LogInformation("Exporting space: {Space}", space);
            var pages = await FetchAllPagesAsync(space, ct);
            all.AddRange(pages);
            logger.LogInformation("  → {Count} pages from {Space}", pages.Count, space);
        }
        await SaveRawAsync(all);
        return all;
    }

    // ── Pagination ─────────────────────────────────────────────────────

    private async Task<List<PageData>> FetchAllPagesAsync(string spaceKey, CancellationToken ct)
    {
        var result = new List<PageData>();
        int start = 0, limit = 100;

        while (true)
        {
            var batch = await FetchBatchAsync(spaceKey, start, limit, ct);
            result.AddRange(batch);
            if (batch.Count < limit) break;
            start += limit;
            if (result.Count >= config.PageLimit) break;
        }
        return result;
    }

    private async Task<List<PageData>> FetchBatchAsync(
        string spaceKey, int start, int limit, CancellationToken ct)
    {
        var status = config.IncludeArchived ? "any" : "current";
        var url = $"{config.BaseUrl.TrimEnd('/')}/rest/api/content" +
                  $"?spaceKey={spaceKey}" +
                  $"&expand=body.storage,version,ancestors,metadata.labels" +
                  $"&start={start}&limit={limit}&status={status}";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        var results = json?["results"]?.AsArray() ?? new JsonArray();

        var pages = new List<PageData>();
        foreach (var r in results)
        {
            if (r is null) continue;
            var page = ParsePage(r);
            if (config.ExportGliffySvg)
            {
                var attachments = await FetchGliffyAttachmentsAsync(page.Id, ct);
                page = page with { Attachments = attachments };
            }
            pages.Add(page);
        }
        return pages;
    }

    // ── Parsers ────────────────────────────────────────────────────────

    private static PageData ParsePage(JsonNode r)
    {
        var ancestors = r["ancestors"]?.AsArray() ?? new JsonArray();
        var breadcrumb = ancestors.Select(a => a?["title"]?.GetValue<string>() ?? "").ToList();
        var parent = ancestors.Count > 0 ? ancestors[^1] : null;

        var labels = r["metadata"]?["labels"]?["results"]?.AsArray()
                      .Select(l => l?["name"]?.GetValue<string>() ?? "")
                      .Where(s => !string.IsNullOrEmpty(s))
                      .ToList() ?? [];

        var version = r["version"];
        return new PageData(
            Id:          r["id"]?.GetValue<string>() ?? "",
            Title:       r["title"]?.GetValue<string>() ?? "",
            SpaceKey:    r["space"]?["key"]?.GetValue<string>() ?? "",
            BodyStorage: r["body"]?["storage"]?["value"]?.GetValue<string>() ?? "",
            Author:      version?["by"]?["displayName"]?.GetValue<string>() ?? "unknown",
            Created:     r["history"]?["createdDate"]?.GetValue<string>() ?? "",
            Updated:     version?["when"]?.GetValue<string>() ?? "",
            Labels:      labels,
            ParentId:    parent?["id"]?.GetValue<string>(),
            ParentTitle: parent?["title"]?.GetValue<string>(),
            Attachments: [],
            Breadcrumb:  breadcrumb
        );
    }

    private async Task<List<AttachmentData>> FetchGliffyAttachmentsAsync(
        string pageId, CancellationToken ct)
    {
        var url = $"{config.BaseUrl.TrimEnd('/')}/rest/api/content/{pageId}" +
                  $"/child/attachment?filename=.gliffy&expand=version";

        var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return [];

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        var attachments = new List<AttachmentData>();

        foreach (var att in json?["results"]?.AsArray() ?? new JsonArray())
        {
            if (att is null) continue;
            var fname = att["title"]?.GetValue<string>() ?? "";
            if (!fname.EndsWith(".gliffy", StringComparison.OrdinalIgnoreCase))
                continue;

            var dlPath = att["_links"]?["download"]?.GetValue<string>() ?? "";
            var dlUrl  = $"{config.BaseUrl.TrimEnd('/')}{dlPath}";
            var dlResp = await _http.GetAsync(dlUrl, ct);
            if (!dlResp.IsSuccessStatusCode) continue;

            var bytes = await dlResp.Content.ReadAsByteArrayAsync(ct);
            attachments.Add(new AttachmentData(
                Filename:      fname,
                ContentBase64: Convert.ToBase64String(bytes),
                MediaType:     att["metadata"]?["mediaType"]?.GetValue<string>() ?? ""
            ));
            logger.LogDebug("  Gliffy: {Filename}", fname);
        }
        return attachments;
    }

    // ── Persistence ────────────────────────────────────────────────────

    private async Task SaveRawAsync(List<PageData> pages)
    {
        Directory.CreateDirectory(config.OutputDir);
        var opts = new JsonSerializerOptions { WriteIndented = true };

        foreach (var page in pages)
        {
            var safe = MakeSafe(page.Title);
            var path = Path.Combine(config.OutputDir, $"{page.Id}_{safe[..Math.Min(60, safe.Length)]}.json");
            await File.WriteAllTextAsync(path,
                JsonSerializer.Serialize(page, opts));
        }

        var index = pages.ToDictionary(
            p => p.Id,
            p => new { p.Title, Space = p.SpaceKey, p.Labels });
        await File.WriteAllTextAsync(
            Path.Combine(config.OutputDir, "_index.json"),
            JsonSerializer.Serialize(index, opts));

        logger.LogInformation("Saved {Count} pages → {Dir}", pages.Count, config.OutputDir);
    }

    private static string MakeSafe(string s) =>
        new string([.. s.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_')]).Trim('_');
}