# Documenter — Confluence-to-Azure Knowledge Pipeline

> **A 5-stage pipeline that exports Confluence wiki pages, converts them to Markdown, categorizes them, generates OpenAPI specs, and uploads everything to Azure (Blob Storage + AI Search).**

---

## Table of Contents

1. [High-Level Architecture](#1-high-level-architecture)
2. [Project Structure](#2-project-structure)
3. [Configuration](#3-configuration)
4. [Pipeline Stages — Detailed Walkthrough](#4-pipeline-stages--detailed-walkthrough)
   - [Stage 1: Confluence Export](#stage-1-confluence-export)
   - [Stage 2: Markdown Conversion](#stage-2-markdown-conversion)
   - [Stage 3: Categorization](#stage-3-categorization)
   - [Stage 4: OpenAPI Spec Generation](#stage-4-openapi-spec-generation)
   - [Stage 5: Azure Upload](#stage-5-azure-upload)
5. [Data Models](#5-data-models)
6. [End-to-End Example](#6-end-to-end-example)
7. [CLI Usage](#7-cli-usage)

---

## 1. High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              Documenter Pipeline                             │
│                                                                              │
│  ┌──────────┐   ┌───────────┐   ┌────────────┐   ┌──────────┐   ┌────────┐ │
│  │ Stage 1  │──▶│  Stage 2  │──▶│  Stage 3   │──▶│ Stage 4  │──▶│Stage 5 │ │
│  │Confluence│   │ Markdown  │   │Categorize  │   │  OAS Gen │   │ Azure  │ │
│  │ Export   │   │ Convert   │   │            │   │          │   │Upload  │ │
│  └────┬─────┘   └─────┬─────┘   └─────┬──────┘   └────┬─────┘   └───┬────┘ │
│       │               │               │               │             │       │
│       ▼               ▼               ▼               ▼             ▼       │
│  output/raw/     output/markdown/ output/categorized/ output/openapi/       │
│  *.json          *.md             api/*.md             *.yaml               │
│  _index.json                      runbook/*.md         Azure Blob Storage   │
│                                   architecture/*.md    Azure AI Search      │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```
  Confluence REST API
        │
        ▼
  ┌─────────────────┐
  │ ExportSpacesAsync│  HTTP GET /rest/api/content?spaceKey=...
  │                  │  Paginated (100/batch, up to PageLimit)
  │  + Gliffy fetch  │  Optional: downloads .gliffy attachments
  └────────┬─────────┘
           │ List<PageData>  →  saved as individual .json + _index.json
           ▼
  ┌─────────────────┐
  │  ConvertAllAsync │  Reads each .json, parses XHTML body
  │                  │  Converts: headings, lists, tables, links, macros
  │  + Gliffy → SVG  │  Outputs: YAML front-matter + Markdown body
  └────────┬─────────┘
           │ List<string>  (file paths)
           ▼
  ┌──────────────────┐
  │CategorizeAllAsync│  Rule-based keyword + title scoring
  │                  │  Categories: api, architecture, runbook, adr, glossary
  │                  │  Copies to output/categorized/{category}/
  └────────┬─────────┘
           │ List<CategoryResult>
           ▼
  ┌─────────────────┐
  │ GenerateAllAsync │  Regex extraction of HTTP endpoints from Markdown
  │                  │  Infers schemas from tables, detects path params
  │                  │  Outputs YAML OpenAPI 3.x specs (per-service or monolith)
  └────────┬─────────┘
           │ List<string>  (spec file paths)
           ▼
  ┌─────────────────┐
  │     RunAsync     │  Uploads .md → Blob container "docs-raw"
  │                  │  Uploads .yaml → Blob container "docs-openapi"
  │                  │  Chunks markdown (sliding window) → Azure AI Search
  │                  │  Optionally triggers an Azure Search indexer
  └─────────────────┘
```

---

## 2. Project Structure

```
documenter/
├── Program.cs                                # Entry point, DI, CLI arg parsing, stage dispatcher
├── documenter.csproj                         # .NET 8 project, NuGet references
│
├── ConfluencePipeline/
│   ├── ConfluenceExporter.cs                 # Stage 1 — Confluence REST API client
│   └── Config/
│       ├── Pipelines.cs                      # All configuration classes (PipelineConfig, etc.)
│       └── Models.cs                         # All data record types (PageData, DocChunk, etc.)
│
├── Markdown/
│   ├── MarkdownConverter.cs                  # Stage 2 — XHTML → Markdown conversion
│   └── Categorize/
│       └── MarkdownCategorizer.cs            # Stage 3 — Rule-based document categorization
│
├── Oas/
│   └── OasGenerator.cs                       # Stage 4 — Markdown → OpenAPI spec generation
│
├── Upload/
│   └── AzureUploader.cs                      # Stage 5 — Azure Blob + AI Search upload
│
└── exporter.cs                               # (Legacy/duplicate of ConfluenceExporter)
```

### Dependencies (NuGet)

| Package | Purpose |
|---|---|
| `Azure.Identity` | DefaultAzureCredential for passwordless Azure auth |
| `Azure.Search.Documents` | Azure AI Search index management & document upload |
| `Azure.Storage.Blobs` | Azure Blob Storage upload |
| `HtmlAgilityPack` | Confluence XHTML body parsing |
| `YamlDotNet` | OpenAPI YAML serialization |
| `Newtonsoft.Json` | JSON utilities |
| `Microsoft.Extensions.Configuration.Json` | JSON config file binding |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | Environment variable overrides |
| `Microsoft.Extensions.Logging.Console` | Console logging |

---

## 3. Configuration

All configuration lives in a single JSON file (default: `pipeline.json`). Environment variables prefixed with `PIPELINE_` override any value.

### Full Configuration Schema

```jsonc
{
  // ── Stage 1: Confluence Export ──
  "Confluence": {
    "BaseUrl":         "https://your-instance.atlassian.net/wiki",
    "AuthToken":       "base64-encoded-user:token",    // or a bearer token
    "AuthType":        "basic",                         // "basic" | "bearer"
    "SpaceKeys":       ["ENG", "OPS", "PLATFORM"],
    "OutputDir":       "./output/raw",
    "PageLimit":       500,
    "IncludeArchived": false,
    "ExportGliffySvg": true
  },

  // ── Stage 2: Markdown Conversion ──
  "Markdown": {
    "InputDir":      "./output/raw",
    "OutputDir":     "./output/markdown",
    "AssetsDir":     "./output/markdown/assets",
    "GliffyMode":   "svg-file",     // "svg-inline" | "svg-file" | "png"
    "AddBreadcrumb": true
  },

  // ── Stage 3: Categorization ──
  "Categorize": {
    "InputDir":       "./output/markdown",
    "OutputDir":      "./output/categorized",
    "TagStrategy":    "rule-based",
    "MinConfidence":  0.70,
    "MetadataOutput": "front-matter",  // "front-matter" | "sidecar"
    "GenerateIndex":  true
  },

  // ── Stage 4: OpenAPI Generation ──
  "OpenApi": {
    "InputDir":         "./output/categorized/api",
    "OutputDir":        "./output/openapi",
    "OasVersion":       "3.1.0",
    "SplitMode":        "per-service",  // "per-service" | "monolith"
    "SchemaInference":  "both",         // "tables" | "code-blocks" | "both"
    "InfoTitle":        "Enterprise API",
    "InfoVersion":      "1.0.0",
    "MarkInternal":     true
  },

  // ── Stage 5: Azure Upload ──
  "Azure": {
    "SearchEndpoint":   "https://your-search.search.windows.net",
    "SearchIndex":      "docs-knowledge-base",
    "BlobAccountUrl":   "https://youraccount.blob.core.windows.net",
    "BlobContainer":    "docs-raw",
    "OasBlobContainer": "docs-openapi",
    "MarkdownDir":      "./output/categorized",
    "OpenApiDir":       "./output/openapi",
    "ChunkSize":        512,
    "ChunkOverlap":     64,
    "TriggerIndexer":   false,
    "IndexerName":      ""
  }
}
```

---

## 4. Pipeline Stages — Detailed Walkthrough

---

### Stage 1: Confluence Export

**Class:** `ConfluenceExporter`  
**Input:** Confluence REST API  
**Output:** `output/raw/*.json` + `output/raw/_index.json`

#### Workflow

```
For each SpaceKey in config:
  │
  ├─▶ FetchAllPagesAsync(spaceKey)
  │     │
  │     └─▶ Loop: FetchBatchAsync(start=0, limit=100)
  │           │
  │           ├─ GET /rest/api/content?spaceKey=X&expand=body.storage,version,ancestors,metadata.labels
  │           ├─ Parse each JSON result → PageData record
  │           ├─ If ExportGliffySvg:
  │           │     └─ FetchGliffyAttachmentsAsync(pageId)
  │           │           GET /rest/api/content/{id}/child/attachment?filename=.gliffy
  │           │           Download binary → Base64
  │           ├─ Repeat until batch.Count < limit or PageLimit reached
  │           └─ Return List<PageData>
  │
  └─▶ SaveRawAsync(allPages)
        ├─ Each page → {id}_{safe-title}.json
        └─ All pages → _index.json (id → {Title, Space, Labels})
```

#### Authentication

- **Bearer**: `Authorization: Bearer <token>` — for PATs or OAuth tokens
- **Basic**: `Authorization: Basic <base64>` — for `user@email:api-token` (Atlassian Cloud)

#### Key Behaviors

- Paginated with a batch size of 100
- Configurable max pages via `PageLimit`
- Optionally includes archived pages (`IncludeArchived`)
- Gliffy diagrams downloaded as binary and stored Base64-encoded in `AttachmentData`
- File names are sanitized: non-alphanumeric characters replaced with `_`, truncated to 60 chars

---

### Stage 2: Markdown Conversion

**Class:** `MarkdownConverter`  
**Input:** `output/raw/*.json`  
**Output:** `output/markdown/*.md` + `output/markdown/assets/*.svg`

#### Workflow

```
BuildIndex()
  └─ Read _index.json → map page IDs to titles & filenames (for cross-links)

For each .json file (excluding _index.json):
  │
  ├─ Deserialize → PageData
  ├─ Build YAML front-matter (title, id, space, author, dates, labels, parent)
  ├─ Optionally add breadcrumb HTML comment
  │
  └─ XhtmlToMarkdown(body)
       │
       ├─ HandleGliffy: replace <ac:structured-macro ac:name="gliffy">
       │     ├─ svg-inline: embed SVG directly in Markdown
       │     ├─ svg-file:   write .svg to assets/, insert ![alt](assets/X.svg)
       │     └─ fallback:   > **[Diagram: name]**
       │
       ├─ HandleMacros: convert Confluence macros
       │     ├─ "code"    → fenced code block (```lang ... ```)
       │     ├─ "info"    → blockquote with ℹ Info prefix
       │     ├─ "warning" → blockquote with ⚠️ Warning prefix
       │     └─ Unsupported (pagetree, children, etc.) → HTML comment
       │
       ├─ HandleLinks: resolve <ac:link><ri:page> to [text](file.md)
       │
       └─ NodeToMarkdown: recursive HTML→Markdown renderer
             h1-h6 → # headings
             p → paragraphs
             strong/b → **bold**
             em/i → _italic_
             code → `inline code`
             a → [text](href)
             ul/ol → - / 1. lists
             table → pipe-delimited table with header separator
             br → double-space newline
             hr → ---
```

#### Example Front-Matter Output

```yaml
---
title: "User Authentication API"
confluence_id: "12345678"
space: "ENG"
author: "Jane Doe"
created: "2024-03-15"
updated: "2025-01-10"
parent: "Platform Services"
labels:
  - api
  - authentication
---
```

---

### Stage 3: Categorization

**Class:** `MarkdownCategorizer`  
**Input:** `output/markdown/*.md`  
**Output:** `output/categorized/{category}/*.md` + `_category_index.json`

#### Categories & Rules

| Category | Keywords (sample) | Title Patterns | Weight |
|---|---|---|---|
| **api** | endpoint, rest, graphql, http, payload, swagger, curl, bearer | `/api/i`, `/endpoint/i`, `/service/i` | 1.0 |
| **architecture** | architecture, diagram, microservice, kafka, database, cloud | `/arch/i`, `/design/i`, `/infra/i` | 1.0 |
| **runbook** | runbook, incident, on-call, alert, troubleshoot, rollback | `/runbook/i`, `/incident/i`, `/procedure/i` | 1.1 |
| **adr** | decision record, adr, status: accepted, consequences | `/adr[-\s]?\d+/i`, `/decision[-\s]record/i` | 1.2 |
| **glossary** | glossary, definition, terminology, abbreviation | `/glossary/i`, `/terms/i` | 1.1 |

#### Scoring Algorithm

```
For each category:
  keyword_score   = count(keyword matches in body+title) × 0.05
  title_score     = count(title pattern matches)          × 0.35
  weighted_score  = (keyword_score + title_score)         × category_weight

confidence = best_score / sum(all_scores)

If confidence < MinConfidence (default 0.70) → "uncategorized"
```

#### Workflow

```
For each .md file:
  │
  ├─ Split front-matter / body
  ├─ Extract title (from front-matter, then H1, then filename)
  ├─ Score against all category rules
  ├─ Pick best category if confidence ≥ threshold
  │
  ├─ If MetadataOutput == "front-matter":
  │     Inject `category:` and `category_confidence:` into YAML block
  │     Write to output/categorized/{category}/{file}.md
  │
  └─ If MetadataOutput == "sidecar":
        Copy .md as-is
        Write .meta.json alongside it

Write _category_index.json:
  { "api": [{title, file, confidence}, ...], "runbook": [...], ... }
```

---

### Stage 4: OpenAPI Spec Generation

**Class:** `OasGenerator`  
**Input:** `output/categorized/api/*.md`  
**Output:** `output/openapi/*.yaml`

#### Workflow

```
If SplitMode == "monolith":
  └─ Generate one spec from ALL .md files

If SplitMode == "per-service":
  │
  ├─ For each .md file: detect service name
  │     1. Match regex: service:\s+ServiceName
  │     2. Fallback: first H1 heading
  │     3. Default: "api"
  │
  ├─ Group files by service name
  │
  └─ For each service group:
       │
       ├─ ExtractEndpoints(text) for each file
       │     Match pattern:  `GET /api/users/{id}`
       │     Extract: method, path, summary (from heading above)
       │     Extract: description (text below endpoint)
       │     Extract: path parameters ({id} → name="id", in="path")
       │     Extract: request schema (from Markdown table below)
       │     Detect: x-internal flag (if "internal" appears nearby)
       │
       └─ BuildOas(service, endpoints) → YAML
            openapi: "3.1.0"
            info: { title: service, version: "1.0.0" }
            paths:
              /api/users/{id}:
                get:
                  summary: "..."
                  tags: [service]
                  parameters: [{ name: id, in: path }]
                  responses: { 200, 400, 401, 500 }
            security: [BearerAuth]
```

#### Schema Inference from Tables

When a Markdown table appears near an endpoint definition:

```markdown
| Field     | Type   | Description           |
|-----------|--------|-----------------------|
| `name`    | string | User's display name   |
| `email`   | string | Email (required)      |
| `age`     | int    | Age in years          |
```

This becomes:

```yaml
schema:
  type: object
  properties:
    name:  { type: string, description: "User's display name" }
    email: { type: string, description: "Email (required)" }
    age:   { type: number, description: "Age in years" }
  required: [email]
```

---

### Stage 5: Azure Upload

**Class:** `AzureUploader`  
**Input:** `output/categorized/**/*.md` + `output/openapi/*.yaml`  
**Output:** Azure Blob Storage + Azure AI Search index

#### Workflow

```
EnsureIndexAsync()
  └─ Check if index exists; if not, create with schema:
       id (key), content (searchable, en-microsoft analyzer),
       title, sourceFile, category, space, author,
       chunkIndex, totalChunks
       + Semantic Search config (title + content prioritized)

UploadBlobsAsync(markdownFiles → "docs-raw" container)
UploadBlobsAsync(oasFiles      → "docs-openapi" container)

ChunkMarkdown(each .md file):
  ├─ Parse front-matter → metadata (title, category, space, author)
  ├─ Strip front-matter from body
  ├─ Split body into words
  └─ Sliding window: size=512 words, overlap=64 words
       Each window → DocChunk with SHA-256 ID

BatchIndexAsync(all chunks → Azure AI Search):
  └─ Upload in batches of 100 documents

If TriggerIndexer:
  └─ POST /indexers/{name}/run (triggers Azure Search indexer)

Return summary: { markdown_files, oas_files, blobs_uploaded, chunks_indexed }
```

#### Chunking Visualization

```
Document body (1200 words):
┌───────────────────────────────────────────────────────────────────┐
│ word₁ word₂ ... word₅₁₂                                         │  ← Chunk 0
│                      word₄₄₉ ... word₉₆₀                        │  ← Chunk 1 (overlap=64)
│                                        word₈₉₇ ... word₁₂₀₀     │  ← Chunk 2
└───────────────────────────────────────────────────────────────────┘

step = ChunkSize - ChunkOverlap = 512 - 64 = 448
```

#### Azure AI Search Index Schema

| Field | Type | Key | Filterable | Facetable | Searchable | Analyzer |
|---|---|---|---|---|---|---|
| `id` | String | ✅ | ✅ | | | |
| `content` | String | | | | ✅ | en.microsoft |
| `title` | String | | | | ✅ | |
| `sourceFile` | String | | ✅ | | | |
| `category` | String | | ✅ | ✅ | | |
| `space` | String | | ✅ | ✅ | | |
| `author` | String | | ✅ | | | |
| `chunkIndex` | Int32 | | ✅ | | | |
| `totalChunks` | Int32 | | | | | |

---

## 5. Data Models

### `PageData` (Confluence page)

```
PageData
├── Id:          string       # Confluence page ID
├── Title:       string       # Page title
├── SpaceKey:    string       # e.g. "ENG"
├── BodyStorage: string       # Raw XHTML (Confluence storage format)
├── Author:      string       # Display name of last editor
├── Created:     string       # ISO date
├── Updated:     string       # ISO date
├── Labels:      List<string> # Confluence labels
├── ParentId:    string?      # Parent page ID
├── ParentTitle: string?      # Parent page title
├── Attachments: List<AttachmentData>
└── Breadcrumb:  List<string> # Ancestor titles (root → parent)
```

### `AttachmentData`

```
AttachmentData
├── Filename:      string   # e.g. "system-diagram.gliffy"
├── ContentBase64: string   # Binary content, base64-encoded
└── MediaType:     string   # MIME type
```

### `CategoryResult`

```
CategoryResult
├── File:       string   # Source .md path
├── Dest:       string   # Destination path (categorized)
├── Title:      string   # Extracted title
├── Category:   string   # e.g. "api", "runbook", "uncategorized"
└── Confidence: double   # 0.0–1.0
```

### `ParsedEndpoint`

```
ParsedEndpoint
├── Method:        string                       # GET, POST, PUT, PATCH, DELETE
├── Path:          string                       # /api/users/{id}
├── Summary:       string                       # From nearest heading
├── Description:   string                       # From text below endpoint
├── Parameters:    List<OasParameter>           # Path parameters
├── RequestSchema: Dictionary<string,object>?   # Inferred from tables
└── IsInternal:    bool                         # If "internal" appears in context
```

### `DocChunk`

```
DocChunk
├── Id:          string   # SHA-256 hash (first 32 hex chars)
├── Content:     string   # Chunk text (up to 512 words)
├── Title:       string   # From front-matter
├── SourceFile:  string   # Original filename
├── Category:    string   # From front-matter
├── Space:       string   # Confluence space key
├── Author:      string   # Author name
├── ChunkIndex:  int      # 0-based chunk position
└── TotalChunks: int      # Total chunks for this file
```

---

## 6. End-to-End Example

### Scenario

You have a Confluence Cloud instance with space `ENG` containing 3 pages:

1. **"User Authentication API"** — documents `POST /api/auth/login` and `GET /api/auth/me`
2. **"System Architecture Overview"** — contains a Gliffy diagram
3. **"Incident Response Runbook"** — step-by-step escalation guide

### Step-by-step Execution

#### 1. Create `pipeline.json`

```json
{
  "Confluence": {
    "BaseUrl": "https://mycompany.atlassian.net/wiki",
    "AuthToken": "dXNlckBjby5jb206eHh4eHh4eHh4eA==",
    "AuthType": "basic",
    "SpaceKeys": ["ENG"]
  },
  "Azure": {
    "SearchEndpoint": "https://myco-search.search.windows.net",
    "BlobAccountUrl": "https://mycostorage.blob.core.windows.net"
  }
}
```

#### 2. Run the full pipeline

```bash
dotnet run -- --config=pipeline.json
```

#### 3. Output per stage

**Stage 1 — Raw JSON export:**
```
output/raw/
├── 11111_User_Authentication_API.json
├── 22222_System_Architecture_Overview.json
├── 33333_Incident_Response_Runbook.json
└── _index.json
```

**Stage 2 — Markdown conversion:**
```
output/markdown/
├── 11111_User_Authentication_API.md
├── 22222_System_Architecture_Overview.md
├── 33333_Incident_Response_Runbook.md
└── assets/
    └── system-diagram.svg
```

Example `11111_User_Authentication_API.md`:
```markdown
---
title: "User Authentication API"
confluence_id: "11111"
space: "ENG"
author: "Jane Doe"
created: "2024-03-15"
updated: "2025-01-10"
parent: "Platform Services"
labels:
  - api
  - authentication
---

# User Authentication API

## Login

POST /api/auth/login

| Field    | Type   | Description          |
|----------|--------|----------------------|
| username | string | User login (required)|
| password | string | Password (required)  |

## Get Current User

GET /api/auth/me

Returns the currently authenticated user profile.
```

**Stage 3 — Categorization:**
```
output/categorized/
├── api/
│   └── 11111_User_Authentication_API.md       (category: api, confidence: 0.85)
├── architecture/
│   └── 22222_System_Architecture_Overview.md  (category: architecture, confidence: 0.78)
├── runbook/
│   └── 33333_Incident_Response_Runbook.md     (category: runbook, confidence: 0.91)
└── _category_index.json
```

**Stage 4 — OpenAPI specs (from `api/` folder only):**
```
output/openapi/
└── user_authentication_api.yaml
```

Generated spec:
```yaml
openapi: "3.1.0"
info:
  title: user-authentication-api
  version: "1.0.0"
paths:
  /api/auth/login:
    post:
      summary: Login
      tags: [user-authentication-api]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                username: { type: string, description: "User login (required)" }
                password: { type: string, description: "Password (required)" }
              required: [username, password]
      responses:
        200: { description: Success }
        400: { description: Bad request }
        401: { description: Unauthorized }
        500: { description: Internal server error }
  /api/auth/me:
    get:
      summary: Get Current User
      tags: [user-authentication-api]
      responses:
        200: { description: Success }
        400: { description: Bad request }
        401: { description: Unauthorized }
        500: { description: Internal server error }
security:
  - BearerAuth: []
```

**Stage 5 — Azure upload:**
```
Azure Blob Storage:
  docs-raw/          ← 3 categorized .md files
  docs-openapi/      ← 1 .yaml spec

Azure AI Search (index: docs-knowledge-base):
  Chunks indexed: ~8 documents (depending on page length)
  Each chunk: 512 words with 64-word overlap
  Semantic search enabled on title + content
```

### Console Output

```
info: Program[0] Running full pipeline (stages 1–5)…
info: Program[0] ==================================================
                  Stage 1
                 ==================================================
info: ConfluenceExporter[0] Exporting space: ENG
info: ConfluenceExporter[0]   → 3 pages from ENG
info: ConfluenceExporter[0] Saved 3 pages → ./output/raw
info: Program[0] Stage 1 complete: 3 pages
info: Program[0] ==================================================
                  Stage 2
                 ==================================================
info: MarkdownConverter[0] Converted 3 files → ./output/markdown
info: Program[0] Stage 2 complete: 3 markdown files
info: Program[0] ==================================================
                  Stage 3
                 ==================================================
info: MarkdownCategorizer[0] Categorized 3 files: api:1, architecture:1, runbook:1
info: Program[0] Stage 3 complete: 3 files categorized
info: Program[0] ==================================================
                  Stage 4
                 ==================================================
info: OasGenerator[0]   user_authentication_api.yaml (2 endpoints)
info: OasGenerator[0] Generated 1 OAS specs → ./output/openapi
info: Program[0] Stage 4 complete: 1 OAS specs
info: Program[0] ==================================================
                  Stage 5
                 ==================================================
info: AzureUploader[0] Index 'docs-knowledge-base' already exists.
info: AzureUploader[0] Uploaded 3 files → blob container 'docs-raw'
info: AzureUploader[0] Uploaded 1 files → blob container 'docs-openapi'
info: AzureUploader[0] Indexed 8 chunks into 'docs-knowledge-base'
info: Program[0] Stage 5 complete: markdown_files=3, oas_files=1, blobs_uploaded=4, chunks_indexed=8
info: Program[0] ✓ Pipeline finished.
```

---

## 7. CLI Usage

```bash
# Run the full pipeline (stages 1 through 5)
dotnet run

# Use a custom config file
dotnet run -- --config=myconfig.json

# Run only a specific stage (e.g., just export)
dotnet run -- --stage=1

# Run only categorization
dotnet run -- --stage=3

# Override config via environment variables
PIPELINE_Confluence__BaseUrl=https://other.atlassian.net/wiki dotnet run

# Cancel gracefully with Ctrl+C (CancellationToken propagated to all stages)
```

### Exit Codes

| Code | Meaning |
|---|---|
| `0` | Pipeline completed successfully |
| `1` | Pipeline failed (exception logged) |

### DI Container

The pipeline uses `Microsoft.Extensions.DependencyInjection`:

```
Singletons (config):       Transients (stages):
  ConfluenceConfig           ConfluenceExporter   → Stage 1
  MarkdownConfig             MarkdownConverter    → Stage 2
  CategoryConfig             MarkdownCategorizer  → Stage 3
  OpenApiConfig              OasGenerator         → Stage 4
  AzureConfig                AzureUploader        → Stage 5
```

Each stage class uses **primary constructor injection** for its config and logger.
