# Converting Markdown to Spec Kit Format

> A practical guide to restructuring existing Markdown documentation into [GitHub Spec Kit](https://github.com/github/spec-kit) artifacts for Spec-Driven Development (SDD).

---

## Table of Contents

1. [What Is Spec Kit?](#1-what-is-spec-kit)
2. [Spec Kit Directory Structure](#2-spec-kit-directory-structure)
3. [Artifact Formats](#3-artifact-formats)
   - [3.1 Constitution](#31-constitution)
   - [3.2 Feature Specification (spec.md)](#32-feature-specification-specmd)
   - [3.3 Implementation Plan (plan.md)](#33-implementation-plan-planmd)
   - [3.4 Task Breakdown (tasks.md)](#34-task-breakdown-tasksmd)
   - [3.5 Supporting Artifacts](#35-supporting-artifacts)
4. [Conversion Rules — Markdown to spec.md](#4-conversion-rules--markdown-to-specmd)
5. [Conversion Rules — Markdown to plan.md](#5-conversion-rules--markdown-to-planmd)
6. [Conversion Rules — Markdown to tasks.md](#6-conversion-rules--markdown-to-tasksmd)
7. [Step-by-Step Conversion Walkthrough](#7-step-by-step-conversion-walkthrough)
8. [Before & After Examples](#8-before--after-examples)
9. [Checklist](#9-checklist)
10. [Quick Reference](#10-quick-reference)

---

## 1. What Is Spec Kit?

Spec Kit is GitHub's open-source toolkit for **Spec-Driven Development** (SDD) — a methodology where specifications are written first, then used to drive implementation through AI coding assistants.

The core workflow has 6 phases:

```
/speckit.constitution → /speckit.specify → /speckit.clarify → /speckit.plan → /speckit.tasks → /speckit.implement
```

| Phase | Command | Purpose |
|---|---|---|
| **Constitution** | `/speckit.constitution` | Project-wide governing principles and standards |
| **Specify** | `/speckit.specify` | Define *what* to build and *why* (user stories + requirements) |
| **Clarify** | `/speckit.clarify` | Identify and resolve ambiguities before planning |
| **Plan** | `/speckit.plan` | Define *how* to build it (tech stack, architecture, data model) |
| **Tasks** | `/speckit.tasks` | Break the plan into actionable, ordered task lists |
| **Implement** | `/speckit.implement` | Execute all tasks to produce the implementation |

**Key principle**: Specifications define the *what* and *why*. Plans define the *how*. Tasks define the *steps*. Never mix them.

---

## 2. Spec Kit Directory Structure

After `specify init`, your project gets this layout:

```
your-project/
├── .specify/
│   ├── memory/
│   │   └── constitution.md         # Project principles & development guidelines
│   ├── scripts/                    # Helper scripts (branch creation, etc.)
│   ├── specs/
│   │   └── 001-feature-name/       # One folder per feature
│   │       ├── spec.md             # Feature specification (what & why)
│   │       ├── plan.md             # Implementation plan (how)
│   │       ├── research.md         # Tech stack research findings
│   │       ├── data-model.md       # Entity/data model design
│   │       ├── quickstart.md       # How to run/test the feature
│   │       ├── contracts/          # API contracts, schemas
│   │       │   ├── api-spec.json
│   │       │   └── signalr-spec.md
│   │       └── tasks.md            # Task breakdown (steps)
│   └── templates/
│       ├── spec-template.md
│       ├── plan-template.md
│       └── tasks-template.md
```

### Feature Numbering

Features are numbered sequentially: `001-create-taskify`, `002-add-dark-mode`, etc. Each feature gets its own Git branch (e.g., `001-create-taskify`).

---

## 3. Artifact Formats

### 3.1 Constitution

The constitution (`constitution.md`) contains project-wide governing principles. It is **not** per-feature — it guides all development.

```markdown
# Project Constitution

## Code Quality
- All public APIs must have documentation
- Functions must be under 50 lines
- No global mutable state

## Testing Standards
- Minimum 80% code coverage
- All API endpoints must have contract tests
- Integration tests for all user journeys

## Performance
- API responses under 200ms at p95
- No N+1 query patterns

## Security
- All inputs must be validated at system boundaries
- Authentication required for all non-public endpoints
- Secrets must never appear in source code
```

**Conversion source**: Extract from README files, CONTRIBUTING.md, coding standards documents, ADRs, or architecture decision records.

---

### 3.2 Feature Specification (`spec.md`)

The spec defines **what** to build and **why**. It contains user stories with acceptance scenarios, functional requirements, success criteria, and assumptions.

#### Template

```markdown
# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`
**Created**: [DATE]
**Status**: Draft
**Input**: User description: "[original requirement text]"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and priority level]

**Independent Test**: [How to verify this story works on its own]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value]

**Independent Test**: [How to verify independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### Edge Cases

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST [specific capability]
- **FR-002**: System MUST [specific capability]
- **FR-003**: Users MUST be able to [key interaction]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes]
- **[Entity 2]**: [What it represents, relationships]

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete X in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users"]

## Assumptions

- [Assumption about users, scope, or environment]
- [Dependency on existing system/service]
```

#### Spec Rules

1. **User stories are prioritized** (P1, P2, P3...) — P1 is the MVP.
2. **Each story must be independently testable** — if you implement only one, it delivers value.
3. **Use Given/When/Then** for acceptance scenarios.
4. **Requirements use FR-### numbering** with MUST/SHOULD/MAY keywords.
5. **Mark unknowns**: `[NEEDS CLARIFICATION: ...]` for anything unspecified.
6. **No tech stack details** in the spec — those belong in `plan.md`.

---

### 3.3 Implementation Plan (`plan.md`)

The plan defines **how** to build the feature — tech stack, architecture, project structure, and data model.

#### Template

```markdown
# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

## Summary

[Primary requirement + technical approach]

## Technical Context

**Language/Version**: [e.g., C# / .NET 8]
**Primary Dependencies**: [e.g., ASP.NET Core, Entity Framework]
**Storage**: [e.g., PostgreSQL, SQLite, files]
**Testing**: [e.g., xUnit, NUnit]
**Target Platform**: [e.g., Linux server, Windows, cross-platform]
**Project Type**: [e.g., web-service, cli, library]
**Performance Goals**: [e.g., 1000 req/s, <200ms p95]
**Constraints**: [e.g., must run offline, <100MB memory]

## Constitution Check

*GATE: Must pass before research. Re-check after design.*

- [✅/❌] [Principle from constitution]: [How this plan complies]

## Project Structure

### Documentation (this feature)

```text
specs/001-feature/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code

```text
src/
├── Models/
├── Services/
├── Controllers/
└── Config/
tests/
├── Unit/
└── Integration/
```
```

---

### 3.4 Task Breakdown (`tasks.md`)

Tasks break the plan into ordered, actionable items organized by user story.

#### Template

```markdown
# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create project structure per plan
- [ ] T002 Initialize project with dependencies
- [ ] T003 [P] Configure linting and formatting

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

- [ ] T004 Setup database schema and migrations
- [ ] T005 [P] Implement authentication framework
- [ ] T006 [P] Setup API routing and middleware

**Checkpoint**: Foundation ready — user story work can begin

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [What this story delivers]
**Independent Test**: [How to verify]

### Implementation for User Story 1

- [ ] T010 [P] [US1] Create Entity model in src/Models/Entity.cs
- [ ] T011 [US1] Implement Service in src/Services/EntityService.cs
- [ ] T012 [US1] Implement endpoint in src/Controllers/EntityController.cs

**Checkpoint**: User Story 1 fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

[Same pattern...]

---

## Phase N: Polish & Cross-Cutting Concerns

- [ ] TXXX [P] Documentation updates
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization
- [ ] TXXX Security hardening

## Dependencies & Execution Order

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all stories
- **User Stories (Phase 3+)**: Depend on Foundational; can run in parallel
- **Polish (Final)**: Depends on all user stories
```

#### Task Rules

1. **Organized by user story** — each story is an independent, deliverable slice.
2. **`[P]` marks parallel tasks** — different files, no dependencies.
3. **`[US#]` maps every task to a story** for traceability.
4. **Include exact file paths** in every task description.
5. **Checkpoints** after each story phase for validation.
6. **Task IDs** are sequential: T001, T002, T003...

---

### 3.5 Supporting Artifacts

| Artifact | File | Purpose |
|---|---|---|
| **Research** | `research.md` | Tech stack investigation, version compatibility, library evaluation |
| **Data Model** | `data-model.md` | Entity definitions, relationships, database schema |
| **Quickstart** | `quickstart.md` | How to build, run, and test the feature locally |
| **API Contracts** | `contracts/api-spec.json` | OpenAPI/JSON schemas for the API surface |

---

## 4. Conversion Rules — Markdown to `spec.md`

Use this mapping to convert existing documentation into a feature specification:

| Source Markdown Content | Spec Kit Target | How to Convert |
|---|---|---|
| Page title / H1 heading | `# Feature Specification: [NAME]` | Use as feature name |
| Introductory paragraphs | `**Input**: User description` | Condense into the "what and why" |
| Feature descriptions | `### User Story N` | One story per user journey; prioritize P1, P2, P3 |
| "User can do X" bullets | `**Acceptance Scenarios**` | Rewrite as **Given**/**When**/**Then** |
| API endpoint docs | `### Functional Requirements` | `FR-001: System MUST accept POST to /api/...` |
| Configuration options | `### Functional Requirements` | `FR-00N: System SHOULD allow configuring X` |
| Error handling notes | `### Edge Cases` | "What happens when [error condition]?" |
| Performance notes | `## Success Criteria` | `SC-001: [Measurable metric]` |
| "We assume X" sentences | `## Assumptions` | List as bullet points |
| Architecture / tech stack details | **REMOVE** from spec | Move to `plan.md` instead |
| Implementation steps | **REMOVE** from spec | Move to `tasks.md` instead |
| YAML front-matter | **REMOVE** | Spec Kit doesn't use front-matter |
| Unclear/missing details | Mark with `[NEEDS CLARIFICATION: ...]` | Flag for `/speckit.clarify` |

### Conversion Pseudocode

```
FOR each markdown file describing a feature:
    1. Create .specify/specs/NNN-feature-name/spec.md
    2. Write header: Feature name, branch, date, status
    3. Extract user journeys → User Stories (P1, P2, P3)
       FOR each user journey:
           a. Write "### User Story N - [Title] (Priority: PN)"
           b. Write plain-language description
           c. Write "**Acceptance Scenarios**:"
           d. Convert each condition to Given/When/Then format
    4. Extract capabilities → Functional Requirements (FR-001, FR-002...)
       Use MUST / SHOULD / MAY keywords
    5. Extract metrics → Success Criteria (SC-001, SC-002...)
    6. Extract assumptions → Assumptions section
    7. DISCARD: tech stack, architecture, implementation details
       (save for plan.md conversion)
```

---

## 5. Conversion Rules — Markdown to `plan.md`

| Source Markdown Content | Plan Target | How to Convert |
|---|---|---|
| Tech stack mentions | `## Technical Context` | Language, dependencies, storage, testing framework |
| Architecture descriptions | `## Project Structure` | Map to file tree format |
| Data model / schema docs | Separate `data-model.md` | Entity definitions and relationships |
| API endpoint structures | Separate `contracts/api-spec.json` | Convert to OpenAPI or structured contract |
| Diagrams / data flows | `## Summary` or `data-model.md` | ASCII diagrams or Mermaid |
| Library/framework choices | `## Technical Context` | List under Primary Dependencies |
| Performance requirements | `## Technical Context` → Performance Goals | Quantify: req/s, latency, memory |

---

## 6. Conversion Rules — Markdown to `tasks.md`

| Source Markdown Content | Tasks Target | How to Convert |
|---|---|---|
| "Step 1, Step 2..." procedures | Task items `- [ ] T00N` | One task per discrete action |
| "Create X component" | `- [ ] T00N [US#] Create X in src/path/file.ext` | Add exact file path |
| "Set up the database" | Phase 2: Foundational task | `- [ ] T00N Setup database schema` |
| "Install dependencies" | Phase 1: Setup task | `- [ ] T00N Initialize project with dependencies` |
| Independent sub-tasks | Mark with `[P]` | `- [ ] T00N [P] [US1] Create model` |
| "Test that X works" | Checkpoint after phase | `**Checkpoint**: [description]` |

---

## 7. Step-by-Step Conversion Walkthrough

### Scenario: Converting a "User Authentication API" document

**Original Markdown:**

```markdown
---
title: "User Authentication API"
space: "ENG"
author: "Jane Doe"
labels:
  - api
  - authentication
---

# User Authentication API

This service handles user login, token issuance, and session management.
Built with ASP.NET Core and uses JWT tokens stored in a Redis cache.

## Login Endpoint

POST /api/auth/login

Accepts username and password. Returns a JWT token on success.
Returns 401 on invalid credentials. Rate-limited to 5 attempts/min/IP.

| Field    | Type   | Description          |
|----------|--------|----------------------|
| username | string | User login (required)|
| password | string | Password (required)  |

## Get Current User

GET /api/auth/me — Returns the profile of the currently authenticated user.
Requires a valid Bearer token.

## Session Expiration

Sessions expire after 24 hours of inactivity.
```

### Step 1: Create the spec (`spec.md`)

```markdown
# Feature Specification: User Authentication

**Feature Branch**: `001-user-authentication`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "Authentication service handling login, token issuance, and session management"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Login (Priority: P1)

A user enters their username and password to log in. On success, they receive
a token that grants access to protected endpoints. On failure, they see an
error message.

**Why this priority**: Login is the entry point — nothing else works without it.

**Independent Test**: Can be verified by submitting credentials to the login
endpoint and checking the response contains a valid token.

**Acceptance Scenarios**:

1. **Given** a user with valid credentials, **When** the user submits POST /api/auth/login with username and password, **Then** a JWT token is returned with status 200
2. **Given** invalid credentials, **When** the user submits POST /api/auth/login, **Then** a 401 Unauthorized response is returned and no token is issued
3. **Given** a client IP that has made 5 login attempts in the last minute, **When** a 6th attempt is made, **Then** a 429 Too Many Requests response is returned

---

### User Story 2 - View Current User Profile (Priority: P2)

An authenticated user retrieves their own profile information using their
token.

**Why this priority**: Core self-service capability needed once login works.

**Independent Test**: After logging in, call GET /api/auth/me and verify the
response contains the user's profile data.

**Acceptance Scenarios**:

1. **Given** a user with a valid Bearer token, **When** GET /api/auth/me is called, **Then** the user's profile is returned
2. **Given** no token or an expired token, **When** GET /api/auth/me is called, **Then** a 401 Unauthorized response is returned

---

### Edge Cases

- What happens when the login request body is missing required fields?
- How does the system handle concurrent login attempts from the same user?
- What happens when the token store (cache) is unavailable?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept POST /api/auth/login with username and password
- **FR-002**: System MUST return a JWT token on successful authentication
- **FR-003**: System MUST return 401 on invalid credentials
- **FR-004**: System MUST expire sessions after 24 hours of inactivity
- **FR-005**: System SHOULD rate-limit login attempts to 5 per minute per IP
- **FR-006**: System MUST return the authenticated user's profile via GET /api/auth/me

### Key Entities

- **User**: Represents an application user (username, email, display name, hashed password)
- **Session**: Represents an active authentication session (token, user ID, expiration time)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete login in under 500ms at p95
- **SC-002**: Invalid login attempts are rejected within 200ms
- **SC-003**: Rate limiting correctly blocks after 5 attempts per minute per IP

## Assumptions

- Users have been pre-registered in the system
- HTTPS is enforced at the infrastructure level
- Mobile support is out of scope for this phase
```

### Step 2: Create the plan (`plan.md`)

```markdown
# Implementation Plan: User Authentication

**Branch**: `001-user-authentication` | **Date**: 2026-04-18 | **Spec**: specs/001-user-authentication/spec.md

## Summary

Build a JWT-based authentication API with login, profile retrieval, session
expiration, and rate limiting using ASP.NET Core with Redis for token storage.

## Technical Context

**Language/Version**: C# / .NET 8
**Primary Dependencies**: ASP.NET Core, Microsoft.AspNetCore.Authentication.JwtBearer, StackExchange.Redis
**Storage**: Redis (session/token cache), SQL Server (user data)
**Testing**: xUnit, WebApplicationFactory for integration tests
**Target Platform**: Linux container (Docker)
**Project Type**: web-service
**Performance Goals**: <500ms p95 for login, <200ms for token validation
**Constraints**: Stateless API; session state in Redis only

## Project Structure

### Source Code

```text
src/
├── Controllers/
│   └── AuthController.cs
├── Models/
│   ├── User.cs
│   ├── LoginRequest.cs
│   └── LoginResponse.cs
├── Services/
│   ├── AuthService.cs
│   ├── TokenService.cs
│   └── RateLimitService.cs
├── Middleware/
│   └── RateLimitMiddleware.cs
└── Config/
    └── JwtSettings.cs

tests/
├── Unit/
│   ├── AuthServiceTests.cs
│   └── TokenServiceTests.cs
└── Integration/
    ├── LoginEndpointTests.cs
    └── ProfileEndpointTests.cs
```
```

### Step 3: Create the tasks (`tasks.md`)

```markdown
# Tasks: User Authentication

**Input**: Design documents from `/specs/001-user-authentication/`
**Prerequisites**: plan.md, spec.md

## Phase 1: Setup

- [ ] T001 Create .NET 8 web API project
- [ ] T002 Add NuGet packages: JwtBearer, StackExchange.Redis, xUnit
- [ ] T003 [P] Configure appsettings.json with JWT and Redis settings

---

## Phase 2: Foundational

- [ ] T004 Create User model in src/Models/User.cs
- [ ] T005 Create LoginRequest and LoginResponse DTOs in src/Models/
- [ ] T006 Create JwtSettings config class in src/Config/JwtSettings.cs
- [ ] T007 Implement TokenService (JWT generation/validation) in src/Services/TokenService.cs
- [ ] T008 Configure JWT authentication middleware in Program.cs

**Checkpoint**: JWT infrastructure in place — endpoints can now use [Authorize]

---

## Phase 3: User Story 1 - User Login (Priority: P1) 🎯 MVP

**Goal**: Users can authenticate and receive a JWT token
**Independent Test**: POST /api/auth/login with valid credentials returns token

### Implementation

- [ ] T009 [US1] Implement AuthService.LoginAsync in src/Services/AuthService.cs
- [ ] T010 [US1] Implement POST /api/auth/login in src/Controllers/AuthController.cs
- [ ] T011 [US1] Add input validation for LoginRequest
- [ ] T012 [P] [US1] Implement RateLimitService in src/Services/RateLimitService.cs
- [ ] T013 [US1] Add RateLimitMiddleware in src/Middleware/RateLimitMiddleware.cs
- [ ] T014 [P] [US1] Write LoginEndpointTests in tests/Integration/LoginEndpointTests.cs

**Checkpoint**: Login flow works end-to-end with rate limiting

---

## Phase 4: User Story 2 - View Profile (Priority: P2)

**Goal**: Authenticated users can retrieve their profile
**Independent Test**: GET /api/auth/me with valid token returns user data

### Implementation

- [ ] T015 [US2] Implement GET /api/auth/me in src/Controllers/AuthController.cs
- [ ] T016 [US2] Add [Authorize] attribute and token claims extraction
- [ ] T017 [P] [US2] Write ProfileEndpointTests in tests/Integration/ProfileEndpointTests.cs

**Checkpoint**: Profile retrieval works with valid and invalid tokens

---

## Phase 5: Polish

- [ ] T018 [P] Add API documentation / Swagger annotations
- [ ] T019 Session expiration cleanup job (24-hour TTL in Redis)
- [ ] T020 [P] Unit tests for AuthService and TokenService

## Dependencies & Execution Order

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup — blocks all stories
- **US1 Login (Phase 3)**: Depends on Foundational
- **US2 Profile (Phase 4)**: Depends on Foundational (can run parallel with US1)
- **Polish (Phase 5)**: After all stories complete
```

---

## 8. Before & After Examples

### Example A: Feature bullets → User Story with acceptance scenarios

**Before (plain Markdown):**
```markdown
## Features
- Export pages from Confluence spaces
- Paginate through results (100 per batch)
- Optionally download Gliffy diagram attachments
- Save pages as JSON files with an index file
```

**After (spec.md):**
```markdown
### User Story 1 - Export Confluence Pages (Priority: P1)

A user initiates an export for one or more Confluence spaces. The system
retrieves all pages with their metadata, body content, and optionally
Gliffy attachments, then saves them as individual JSON files.

**Why this priority**: Export is the pipeline entry point — all other stages depend on it.

**Independent Test**: Run the export against a Confluence space and verify
JSON files appear in the output directory with correct content.

**Acceptance Scenarios**:

1. **Given** one or more space keys are configured, **When** the export runs, **Then** all pages in those spaces are retrieved and saved as individual JSON files
2. **Given** a space with more than 100 pages, **When** the export runs, **Then** pagination retrieves all pages in batches of 100
3. **Given** a configured page limit of N, **When** N pages have been retrieved, **Then** the export stops
4. **Given** Gliffy export is enabled, **When** a page has .gliffy attachments, **Then** the attachments are downloaded and stored with the page data
5. **Given** Gliffy export is disabled, **When** a page has .gliffy attachments, **Then** the attachments are skipped
```

### Example B: Architecture prose → Technical Context in plan.md

**Before:**
```markdown
The pipeline is a .NET 8 console app using Microsoft.Extensions.DependencyInjection.
It reads config from pipeline.json. Azure uploads use DefaultAzureCredential.
HtmlAgilityPack parses Confluence XHTML. YamlDotNet serializes OpenAPI specs.
```

**After (plan.md):**
```markdown
## Technical Context

**Language/Version**: C# / .NET 8
**Primary Dependencies**: Microsoft.Extensions.DependencyInjection, HtmlAgilityPack, YamlDotNet, Azure.Search.Documents, Azure.Storage.Blobs
**Storage**: File system (JSON, Markdown, YAML); Azure Blob Storage; Azure AI Search
**Testing**: [NEEDS CLARIFICATION: no test framework specified]
**Target Platform**: Cross-platform console application
**Project Type**: cli
**Performance Goals**: [NEEDS CLARIFICATION: not specified]
**Constraints**: Requires network access to Confluence and Azure; config via pipeline.json + PIPELINE_ env vars
```

### Example C: Todo list → Task breakdown

**Before:**
```markdown
## Steps
1. Set up the project
2. Create the models
3. Build the exporter
4. Build the converter
5. Test everything
```

**After (tasks.md):**
```markdown
## Phase 1: Setup
- [ ] T001 Create .NET 8 console project with documenter.csproj
- [ ] T002 Add NuGet packages: HtmlAgilityPack, YamlDotNet, Azure.Search.Documents, Azure.Storage.Blobs

## Phase 2: Foundational
- [ ] T003 Create PipelineConfig and section configs in Config/Pipelines.cs
- [ ] T004 [P] Create PageData, AttachmentData, CategoryResult records in Config/Models.cs
- [ ] T005 Configure DI container and CLI arg parsing in Program.cs

## Phase 3: User Story 1 - Confluence Export (P1) 🎯 MVP
- [ ] T006 [US1] Implement ConfluenceExporter.ExportSpacesAsync in ConfluencePipeline/ConfluenceExporter.cs
- [ ] T007 [US1] Implement pagination in FetchAllPagesAsync
- [ ] T008 [P] [US1] Implement Gliffy attachment download in FetchGliffyAttachmentsAsync
- [ ] T009 [US1] Implement SaveRawAsync for JSON persistence

**Checkpoint**: Export produces JSON files from Confluence
```

---

## 9. Checklist

Use this when converting an existing Markdown document to Spec Kit format:

### For `spec.md`:
```markdown
- [ ] Created .specify/specs/NNN-feature-name/spec.md
- [ ] Wrote header: Feature name, branch, date, status, input
- [ ] Extracted user stories with priorities (P1, P2, P3)
- [ ] Each user story has: description, priority rationale, independent test
- [ ] Each user story has Given/When/Then acceptance scenarios
- [ ] Each user story is independently testable and delivers value
- [ ] Listed functional requirements with FR-### IDs and MUST/SHOULD/MAY
- [ ] Listed key entities (if data is involved)
- [ ] Defined measurable success criteria with SC-### IDs
- [ ] Listed assumptions explicitly
- [ ] Flagged unknowns with [NEEDS CLARIFICATION: ...]
- [ ] NO tech stack or implementation details in the spec
- [ ] NO internal class names or library references in the spec
```

### For `plan.md`:
```markdown
- [ ] Created plan.md in the same feature folder
- [ ] Filled in Technical Context (language, dependencies, storage, testing, platform)
- [ ] Defined project structure with file tree
- [ ] Validated against constitution (Constitution Check)
- [ ] Created research.md for tech stack findings (if needed)
- [ ] Created data-model.md for entity definitions (if needed)
- [ ] Created contracts/ with API specs (if needed)
```

### For `tasks.md`:
```markdown
- [ ] Tasks organized by user story (Phase per story)
- [ ] Phase 1 (Setup) and Phase 2 (Foundational) come first
- [ ] Every task has a T### ID
- [ ] Every task has a [US#] story label
- [ ] Parallel tasks marked with [P]
- [ ] Exact file paths in every task description
- [ ] Checkpoints after each story phase
- [ ] Final Polish phase for cross-cutting concerns
- [ ] Dependencies documented at the bottom
```

---

## 10. Quick Reference

### Spec Kit Workflow

```
Constitution (project-wide)
    │
    ▼
spec.md ──► plan.md ──► tasks.md ──► implement
  WHAT         HOW        STEPS        CODE
  WHY       TECH STACK   ORDERED     GENERATED
```

### File Layout per Feature

```
.specify/specs/NNN-feature-name/
├── spec.md          # What & why (user stories, requirements, criteria)
├── plan.md          # How (tech stack, architecture, data model)
├── research.md      # Tech investigation results
├── data-model.md    # Entities and relationships
├── quickstart.md    # Build/run/test instructions
├── contracts/       # API contracts / schemas
└── tasks.md         # Ordered implementation steps
```

### Acceptance Scenario Format

```markdown
**Given** [precondition], **When** [action], **Then** [outcome]
```

### Functional Requirement Format

```markdown
- **FR-001**: System MUST [observable behavior]
- **FR-002**: System SHOULD [recommended behavior]
- **FR-003**: System MAY [optional behavior]
```

### Task Format

```markdown
- [ ] T001 [P] [US1] Create Entity model in src/Models/Entity.cs
        │    │    │    │                      │
        │    │    │    │                      └─ Exact file path
        │    │    │    └─ Task description
        │    │    └─ User story reference
        │    └─ Parallel marker (optional)
        └─ Task ID
```

### Spec Kit CLI

```bash
# Install
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@vX.Y.Z

# Initialize in existing project
specify init . --ai copilot

# Verify setup
specify check

# Slash commands (in your AI agent)
/speckit.constitution    # Set project principles
/speckit.specify         # Create feature spec
/speckit.clarify         # Resolve ambiguities
/speckit.plan            # Create implementation plan
/speckit.tasks           # Generate task breakdown
/speckit.implement       # Execute all tasks
/speckit.analyze         # Cross-artifact consistency check
```
