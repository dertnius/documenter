using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Documenter.ConfluencePipeline.Config;
using Microsoft.Extensions.Logging;

namespace ConfluencePipeline.Stages;

public partial class AzureUploader(AzureConfig config, ILogger<AzureUploader> logger)
{
    [GeneratedRegex(@"---\n(.*?)\n---", RegexOptions.Singleline)]
    private static partial Regex FrontMatterRegex();
    [GeneratedRegex(@"---.*?---\n", RegexOptions.Singleline)]
    private static partial Regex StripFmRegex();

    private readonly DefaultAzureCredential _credential = new();

    // ── Entry point ────────────────────────────────────────────────────

    public async Task<Dictionary<string, int>> RunAsync(CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);

        var mdFiles  = Directory.GetFiles(config.MarkdownDir, "*.md",  SearchOption.AllDirectories);
        var oasFiles = Directory.GetFiles(config.OpenApiDir,  "*.yaml", SearchOption.AllDirectories);

        var mdBlobs  = await UploadBlobsAsync(mdFiles,  config.BlobContainer,    ct);
        var oasBlobs = await UploadBlobsAsync(oasFiles, config.OasBlobContainer, ct);

        var chunks  = mdFiles.SelectMany(f => ChunkMarkdown(f)).ToList();
        var indexed = await BatchIndexAsync(chunks, ct);

        if (config.TriggerIndexer && !string.IsNullOrEmpty(config.IndexerName))
            await TriggerIndexerAsync(ct);

        var summary = new Dictionary<string, int>
        {
            ["markdown_files"]  = mdFiles.Length,
            ["oas_files"]       = oasFiles.Length,
            ["blobs_uploaded"]  = mdBlobs + oasBlobs,
            ["chunks_indexed"]  = indexed,
        };
        logger.LogInformation("Upload complete: {Summary}",
            string.Join(", ", summary.Select(kv => $"{kv.Key}={kv.Value}")));
        return summary;
    }

    // ── Index management ───────────────────────────────────────────────

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        var indexClient = new SearchIndexClient(
            new Uri(config.SearchEndpoint), _credential);

        await foreach (var idx in indexClient.GetIndexesAsync(ct))
            if (idx.Name == config.SearchIndex)
            {
                logger.LogInformation("Index '{Index}' already exists.", config.SearchIndex);
                return;
            }

        logger.LogInformation("Creating index '{Index}'…", config.SearchIndex);
        var index = new SearchIndex(config.SearchIndex)
        {
            Fields =
            [
                new SimpleField("id",          SearchFieldDataType.String)  { IsKey=true, IsFilterable=true },
                new SearchableField("content")  { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SearchableField("title"),
                new SimpleField("sourceFile",   SearchFieldDataType.String)  { IsFilterable=true },
                new SimpleField("category",     SearchFieldDataType.String)  { IsFilterable=true, IsFacetable=true },
                new SimpleField("space",        SearchFieldDataType.String)  { IsFilterable=true, IsFacetable=true },
                new SimpleField("author",       SearchFieldDataType.String)  { IsFilterable=true },
                new SimpleField("chunkIndex",   SearchFieldDataType.Int32)   { IsFilterable=true },
                new SimpleField("totalChunks",  SearchFieldDataType.Int32),
            ],
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new SemanticPrioritizedFields
                    {
                        TitleField   = new SemanticField("title"),
                        ContentFields = { new SemanticField("content") },
                    })
                }
            },
        };

        await indexClient.CreateIndexAsync(index, ct);
        logger.LogInformation("Index created with semantic search.");
    }

    // ── Chunking ───────────────────────────────────────────────────────

    private List<DocChunk> ChunkMarkdown(string mdFile)
    {
        var text  = File.ReadAllText(mdFile, Encoding.UTF8);
        var meta  = ParseFrontMatter(text);
        var body  = StripFmRegex().Replace(text, "", 1);
        var words = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var wordChunks = SlidingWindow(words, config.ChunkSize, config.ChunkOverlap).ToList();
        var chunks = new List<DocChunk>(wordChunks.Count);

        for (int i = 0; i < wordChunks.Count; i++)
        {
            var content = string.Join(" ", wordChunks[i]);
            var rawId   = $"{Path.GetFileName(mdFile)}:{i}";
            var id      = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawId)))[..32]
                               .ToLower();

            chunks.Add(new DocChunk(
                Id:          id,
                Content:     content,
                Title:       meta.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(mdFile)),
                SourceFile:  Path.GetFileName(mdFile),
                Category:    meta.GetValueOrDefault("category",  "uncategorized"),
                Space:       meta.GetValueOrDefault("space",     ""),
                Author:      meta.GetValueOrDefault("author",    ""),
                ChunkIndex:  i,
                TotalChunks: wordChunks.Count
            ));
        }
        return chunks;
    }

    private static IEnumerable<string[]> SlidingWindow(
        string[] words, int size, int overlap)
    {
        int step = Math.Max(1, size - overlap);
        for (int start = 0; start < words.Length; start += step)
            yield return words[start..Math.Min(start + size, words.Length)];
    }

    // ── Indexing ───────────────────────────────────────────────────────

    private async Task<int> BatchIndexAsync(
        List<DocChunk> chunks, CancellationToken ct, int batchSize = 100)
    {
        var searchClient = new SearchClient(
            new Uri(config.SearchEndpoint), config.SearchIndex, _credential);

        int total = 0;
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var batch = chunks[i..Math.Min(i + batchSize, chunks.Count)];
            var docs   = batch.Select(c => new SearchDocument
            {
                ["id"]          = c.Id,
                ["content"]     = c.Content,
                ["title"]       = c.Title,
                ["sourceFile"]  = c.SourceFile,
                ["category"]    = c.Category,
                ["space"]       = c.Space,
                ["author"]      = c.Author,
                ["chunkIndex"]  = c.ChunkIndex,
                ["totalChunks"] = c.TotalChunks,
            });

            var result = await searchClient.UploadDocumentsAsync(docs, cancellationToken: ct);
            var ok = result.Value.Results.Count(r => r.Succeeded);
            total += ok;
            logger.LogDebug("  Batch {N}: {Ok}/{Total}", i / batchSize + 1, ok, batch.Count);
        }

        logger.LogInformation("Indexed {Total} chunks into '{Index}'", total, config.SearchIndex);
        return total;
    }

    // ── Blob Storage ───────────────────────────────────────────────────

    private async Task<int> UploadBlobsAsync(
        string[] files, string container, CancellationToken ct)
    {
        var blobSvc = new BlobServiceClient(new Uri(config.BlobAccountUrl), _credential);
        var cc      = blobSvc.GetBlobContainerClient(container);
        await cc.CreateIfNotExistsAsync(cancellationToken: ct);

        int count = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            await cc.GetBlobClient(Path.GetFileName(file))
                    .UploadAsync(stream, overwrite: true, cancellationToken: ct);
            count++;
            logger.LogDebug("  Blob: {Container}/{File}", container, Path.GetFileName(file));
        }
        logger.LogInformation("Uploaded {Count} files → blob container '{Container}'",
            count, container);
        return count;
    }

    // ── Indexer trigger ────────────────────────────────────────────────

    private async Task TriggerIndexerAsync(CancellationToken ct)
    {
        var token = (await _credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://search.azure.com/.default"]), ct)).Token;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var url = $"{config.SearchEndpoint}/indexers/{config.IndexerName}/run?api-version=2023-11-01";
        var resp = await http.PostAsync(url, null, ct);
        if (resp.IsSuccessStatusCode)
            logger.LogInformation("Indexer '{Name}' triggered.", config.IndexerName);
        else
            logger.LogWarning("Indexer trigger failed: {Status}", resp.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static Dictionary<string, string> ParseFrontMatter(string text)
    {
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var m = FrontMatterRegex().Match(text);
        if (!m.Success) return meta;
        foreach (var line in m.Groups[1].Value.Split('\n'))
        {
            var idx = line.IndexOf(':', StringComparison.Ordinal);
            if (idx < 0) continue;
            meta[line[..idx].Trim()] = line[(idx + 1)..].Trim().Trim('"', '\'');
        }
        return meta;
    }
}