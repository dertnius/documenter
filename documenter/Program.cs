using ConfluencePipeline.Stages;
using Documenter.ConfluencePipeline;
using Documenter.ConfluencePipeline.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ── Args ──────────────────────────────────────────────────────────────
var configPath = args.FirstOrDefault(a => a.StartsWith("--config="))?[9..] ?? "pipeline.json";
var stageArg   = args.FirstOrDefault(a => a.StartsWith("--stage="))?[8..];
int? onlyStage = stageArg is not null ? int.Parse(stageArg) : null;

// ── Config ────────────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false)
    .AddEnvironmentVariables("PIPELINE_")
    .Build();

var pipelineConfig = configuration.Get<PipelineConfig>()
    ?? throw new InvalidOperationException($"Could not parse {configPath}");

// ── DI ────────────────────────────────────────────────────────────────
var services = new ServiceCollection();
services.AddLogging(b => b
    .AddConsole(o => o.FormatterName = "simple")
    .SetMinimumLevel(LogLevel.Information));

services.AddSingleton(pipelineConfig.Confluence);
services.AddSingleton(pipelineConfig.Markdown);
services.AddSingleton(pipelineConfig.Categorize);
services.AddSingleton(pipelineConfig.OpenApi);
services.AddSingleton(pipelineConfig.Azure);

services.AddTransient<ConfluenceExporter>();
services.AddTransient<MarkdownConverter>();
services.AddTransient<MarkdownCategorizer>();
services.AddTransient<OasGenerator>();
services.AddTransient<AzureUploader>();

var sp = services.BuildServiceProvider();

// ── Pipeline runner ───────────────────────────────────────────────────
var logger = sp.GetRequiredService<ILogger<Program>>();
var cts    = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    if (onlyStage.HasValue)
    {
        logger.LogInformation("Running stage {Stage} only…", onlyStage);
        await RunStage(onlyStage.Value, sp, logger, cts.Token);
    }
    else
    {
        logger.LogInformation("Running full pipeline (stages 1–5)…");
        for (int s = 1; s <= 5; s++)
        {
            logger.LogInformation("{Bar}\n Stage {Stage}\n{Bar}", new string('=', 50), s, new string('=', 50));
            await RunStage(s, sp, logger, cts.Token);
        }
    }
    logger.LogInformation("✓ Pipeline finished.");
}
catch (OperationCanceledException)
{
    logger.LogWarning("Pipeline cancelled.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Pipeline failed.");
    return 1;
}
return 0;

// ── Stage dispatcher ──────────────────────────────────────────────────
static async Task RunStage(int stage, IServiceProvider sp,
    ILogger logger, CancellationToken ct)
{
    switch (stage)
    {
        case 1:
            var pages = await sp.GetRequiredService<ConfluenceExporter>()
                                 .ExportSpacesAsync(ct);
            logger.LogInformation("Stage 1 complete: {Count} pages", pages.Count);
            break;
        case 2:
            var files = await sp.GetRequiredService<MarkdownConverter>()
                                 .ConvertAllAsync(ct);
            logger.LogInformation("Stage 2 complete: {Count} markdown files", files.Count);
            break;
        case 3:
            var results = await sp.GetRequiredService<MarkdownCategorizer>()
                                   .CategorizeAllAsync(ct);
            logger.LogInformation("Stage 3 complete: {Count} files categorized", results.Count);
            break;
        case 4:
            var specs = await sp.GetRequiredService<OasGenerator>()
                                 .GenerateAllAsync(ct);
            logger.LogInformation("Stage 4 complete: {Count} OAS specs", specs.Count);
            break;
        case 5:
            var summary = await sp.GetRequiredService<AzureUploader>()
                                   .RunAsync(ct);
            logger.LogInformation("Stage 5 complete: {Summary}",
                string.Join(", ", summary.Select(kv => $"{kv.Key}={kv.Value}")));
            break;
        default:
            throw new ArgumentException($"Invalid stage: {stage}");
    }
}