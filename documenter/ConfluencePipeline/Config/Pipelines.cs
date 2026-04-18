namespace Documenter.ConfluencePipeline.Config;

public class PipelineConfig
{
    public ConfluenceConfig Confluence { get; set; } = new();
    public MarkdownConfig   Markdown   { get; set; } = new();
    public CategoryConfig   Categorize { get; set; } = new();
    public OpenApiConfig    OpenApi    { get; set; } = new();
    public AzureConfig      Azure      { get; set; } = new();
}

public class ConfluenceConfig
{
    public string   BaseUrl          { get; set; } = "";
    public string   AuthToken        { get; set; } = "";
    public string[] SpaceKeys        { get; set; } = [];
    public string   OutputDir        { get; set; } = "./output/raw";
    public int      PageLimit        { get; set; } = 500;
    public bool     IncludeArchived  { get; set; } = false;
    public bool     ExportGliffySvg  { get; set; } = true;
    public string   AuthType         { get; set; } = "bearer";  // bearer | basic
}

public class MarkdownConfig
{
    public string InputDir      { get; set; } = "./output/raw";
    public string OutputDir     { get; set; } = "./output/markdown";
    public string GliffyMode    { get; set; } = "svg-file"; // svg-inline | svg-file | png
    public string AssetsDir     { get; set; } = "./output/markdown/assets";
    public bool   AddBreadcrumb { get; set; } = true;
}

public class CategoryConfig
{
    public string InputDir       { get; set; } = "./output/markdown";
    public string OutputDir      { get; set; } = "./output/categorized";
    public string TagStrategy    { get; set; } = "rule-based";
    public double MinConfidence  { get; set; } = 0.70;
    public string MetadataOutput { get; set; } = "front-matter"; // front-matter | sidecar
    public bool   GenerateIndex  { get; set; } = true;
}

public class OpenApiConfig
{
    public string InputDir         { get; set; } = "./output/categorized/api";
    public string OutputDir        { get; set; } = "./output/openapi";
    public string OasVersion       { get; set; } = "3.1.0";
    public string SplitMode        { get; set; } = "per-service"; // per-service | monolith
    public string SchemaInference  { get; set; } = "both";        // tables | code-blocks | both
    public string InfoTitle        { get; set; } = "Enterprise API";
    public string InfoVersion      { get; set; } = "1.0.0";
    public bool   MarkInternal     { get; set; } = true;
}

public class AzureConfig
{
    public string SearchEndpoint    { get; set; } = "";
    public string SearchIndex       { get; set; } = "docs-knowledge-base";
    public string BlobAccountUrl    { get; set; } = "";
    public string BlobContainer     { get; set; } = "docs-raw";
    public string OasBlobContainer  { get; set; } = "docs-openapi";
    public string MarkdownDir       { get; set; } = "./output/categorized";
    public string OpenApiDir        { get; set; } = "./output/openapi";
    public int    ChunkSize         { get; set; } = 512;
    public int    ChunkOverlap      { get; set; } = 64;
    public bool   TriggerIndexer    { get; set; } = false;
    public string IndexerName       { get; set; } = "";
}