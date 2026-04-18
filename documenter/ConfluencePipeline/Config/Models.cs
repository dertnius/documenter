namespace Documenter.ConfluencePipeline.Config;

// ── Confluence ──────────────────────────────────────────────────────────
public record PageData(
    string Id,
    string Title,
    string SpaceKey,
    string BodyStorage,
    string Author,
    string Created,
    string Updated,
    List<string> Labels,
    string? ParentId,
    string? ParentTitle,
    List<AttachmentData> Attachments,
    List<string> Breadcrumb
);

public record AttachmentData(
    string Filename,
    string ContentBase64,
    string MediaType
);

// ── Categorization ───────────────────────────────────────────────────────
public record CategoryResult(
    string File,
    string Dest,
    string Title,
    string Category,
    double Confidence
);

// ── OAS ──────────────────────────────────────────────────────────────────
public record ParsedEndpoint(
    string Method,
    string Path,
    string Summary,
    string Description,
    List<OasParameter> Parameters,
    Dictionary<string, object>? RequestSchema,
    bool IsInternal
);

public record OasParameter(
    string Name,
    string In,
    bool Required,
    string Type
);

// ── Azure ────────────────────────────────────────────────────────────────
public record DocChunk(
    string Id,
    string Content,
    string Title,
    string SourceFile,
    string Category,
    string Space,
    string Author,
    int ChunkIndex,
    int TotalChunks
);