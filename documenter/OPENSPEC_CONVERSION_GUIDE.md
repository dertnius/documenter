# Converting Markdown to OpenSpec Format

> A practical guide to restructuring existing Markdown documentation into the [OpenSpec](https://openspec.dev/) spec-driven format used by AI coding assistants.

---

## Table of Contents

1. [What Is OpenSpec?](#1-what-is-openspec)
2. [OpenSpec Directory Structure](#2-openspec-directory-structure)
3. [The Spec Format](#3-the-spec-format)
4. [The Change Format](#4-the-change-format)
5. [Conversion Rules — Markdown to Spec](#5-conversion-rules--markdown-to-spec)
6. [Conversion Rules — Markdown to Change Artifacts](#6-conversion-rules--markdown-to-change-artifacts)
7. [Step-by-Step Conversion Walkthrough](#7-step-by-step-conversion-walkthrough)
8. [Before & After Examples](#8-before--after-examples)
9. [Checklist](#9-checklist)
10. [Quick Reference](#10-quick-reference)

---

## 1. What Is OpenSpec?

OpenSpec is a lightweight spec-driven framework for AI coding assistants. It organizes project knowledge into two areas:

- **Specs** (`openspec/specs/`) — The source of truth describing how your system *currently* behaves.
- **Changes** (`openspec/changes/`) — Proposed modifications, each in its own folder with artifacts (proposal, design, tasks, delta specs).

The key ideas:

| Concept | Meaning |
|---|---|
| **Requirement** | A specific behavior the system must have (uses RFC 2119 keywords: SHALL, MUST, SHOULD, MAY) |
| **Scenario** | A concrete, testable example of a requirement in GIVEN/WHEN/THEN format |
| **Delta spec** | A spec describing what's ADDED, MODIFIED, or REMOVED relative to the current specs |
| **Artifact** | A document within a change: `proposal.md`, `design.md`, `tasks.md`, or delta `specs/` |

---

## 2. OpenSpec Directory Structure

```
your-project/
└── openspec/
    ├── config.yaml                   # Project config (optional)
    ├── specs/                        # Source of truth
    │   ├── auth/
    │   │   └── spec.md               # Authentication behavior
    │   ├── export/
    │   │   └── spec.md               # Data export behavior
    │   └── upload/
    │       └── spec.md               # Upload behavior
    └── changes/                      # Proposed modifications
        ├── add-dark-mode/
        │   ├── proposal.md           # Why and what
        │   ├── design.md             # Technical approach
        │   ├── tasks.md              # Implementation checklist
        │   └── specs/                # Delta specs
        │       └── auth/
        │           └── spec.md       # What's changing in auth
        └── archive/                  # Completed changes
            └── 2025-01-24-add-2fa/
                └── ...
```

### Domain Organization

Group specs by **domain** — logical areas of your system:

| Pattern | Example |
|---|---|
| By feature area | `auth/`, `payments/`, `search/` |
| By component | `api/`, `frontend/`, `workers/` |
| By bounded context | `ordering/`, `fulfillment/`, `inventory/` |
| By pipeline stage | `export/`, `conversion/`, `categorization/`, `generation/`, `upload/` |

---

## 3. The Spec Format

A spec file (`spec.md`) contains **requirements** and their **scenarios**.

### Template

```markdown
# {Domain} Specification

## Purpose
{One or two sentences explaining what this spec covers.}

## Requirements

### Requirement: {Behavior Name}
The system {SHALL|MUST|SHOULD|MAY} {observable behavior}.

#### Scenario: {Happy path name}
- GIVEN {precondition}
- WHEN {action or event}
- THEN {expected outcome}
- AND {additional outcome}

#### Scenario: {Edge case name}
- GIVEN {precondition}
- WHEN {action or event}
- THEN {expected outcome}

### Requirement: {Another Behavior}
The system MUST {another observable behavior}.

#### Scenario: {Name}
- GIVEN {precondition}
- WHEN {action or event}
- THEN {expected outcome}
```

### Rules

1. **Requirements are the "what"** — state observable behavior, not implementation details.
2. **Scenarios are the "when"** — concrete examples that could be turned into tests.
3. **Use RFC 2119 keywords** to communicate intent:
   - **MUST / SHALL** — absolute requirement, no exceptions.
   - **SHOULD** — recommended, but exceptions may exist with justification.
   - **MAY** — truly optional.
4. **Avoid in specs**: internal class names, library choices, step-by-step implementation details.
5. **Quick test**: if the implementation can change without changing externally visible behavior, it doesn't belong in the spec.

---

## 4. The Change Format

When proposing a modification, create a change folder with these artifacts:

### 4.1 Proposal (`proposal.md`)

Captures **intent**, **scope**, and **approach**.

```markdown
# Proposal: {Change Name}

## Intent
{What problem are you solving? Why does this matter?}

## Scope
In scope:
- {Thing 1}
- {Thing 2}

Out of scope:
- {Explicitly excluded thing}

## Approach
{High-level description of how you'll solve it.}
```

### 4.2 Delta Specs (`specs/{domain}/spec.md`)

Describe what's **changing** relative to current specs.

```markdown
# Delta for {Domain}

## ADDED Requirements

### Requirement: {New Behavior}
The system MUST {new behavior}.

#### Scenario: {Name}
- GIVEN {precondition}
- WHEN {action}
- THEN {outcome}

## MODIFIED Requirements

### Requirement: {Changed Behavior}
The system SHALL {updated behavior}.
(Previously: {old behavior description})

#### Scenario: {Updated scenario}
- GIVEN {precondition}
- WHEN {action}
- THEN {new outcome}

## REMOVED Requirements

### Requirement: {Deprecated Behavior}
(Reason for removal.)
```

| Section | Meaning | On Archive |
|---|---|---|
| `## ADDED Requirements` | New behavior | Appended to main spec |
| `## MODIFIED Requirements` | Changed behavior | Replaces existing requirement |
| `## REMOVED Requirements` | Deprecated behavior | Deleted from main spec |

### 4.3 Design (`design.md`)

Captures **technical approach** and **architecture decisions**.

````markdown
# Design: {Change Name}

## Technical Approach
{How will this be implemented at a technical level?}

## Architecture Decisions

### Decision: {Decision Title}
{Choice made and reasoning.}

## Data Flow
```
Component A
    │
    ▼
Component B ──► Component C
```

## File Changes
- `src/path/to/file.ts` (new)
- `src/path/to/other.ts` (modified)
````

### 4.4 Tasks (`tasks.md`)

Implementation checklist with checkboxes.

```markdown
# Tasks

## 1. {Group Name}
- [ ] 1.1 {Specific, actionable task}
- [ ] 1.2 {Another task}
- [ ] 1.3 {Another task}

## 2. {Another Group}
- [ ] 2.1 {Task}
- [ ] 2.2 {Task}
```

**Task rules:**
- Group related tasks under headings.
- Use hierarchical numbering (1.1, 1.2, etc.).
- Keep tasks small enough to complete in one session.
- Check off (`- [x]`) as you complete them.

---

## 5. Conversion Rules — Markdown to Spec

Use this table to map existing Markdown content to OpenSpec spec elements:

| Source Markdown | OpenSpec Target | How to Convert |
|---|---|---|
| `# Page Title` | `# {Domain} Specification` | Use the domain name, not the page title |
| Introductory paragraphs | `## Purpose` | Condense to 1–2 sentences |
| "The system does X when Y" | `### Requirement:` + `#### Scenario:` | Extract the behavior as a requirement; make the condition a GIVEN/WHEN/THEN scenario |
| Bullet lists of features | Multiple `### Requirement:` blocks | One requirement per feature |
| API endpoint docs (`POST /api/...`) | `### Requirement:` for each endpoint | "The system SHALL accept POST requests to /api/..." with request/response scenarios |
| Configuration tables | `### Requirement:` for configurable behaviors | "The system SHOULD allow configuring {X} with a default of {Y}" |
| Error handling notes | `#### Scenario:` (edge case) | GIVEN invalid input / WHEN submitted / THEN error returned |
| Diagrams / architecture text | Move to `design.md` (in a change) or keep in prose above requirements | Specs describe *what*, not *how* |
| Step-by-step procedures | `tasks.md` (in a change) or exclude from spec | Implementation steps don't belong in specs |
| YAML front-matter | Remove | OpenSpec doesn't use front-matter in specs |

### Conversion Pseudocode

```
FOR each markdown file:
    1. Determine the DOMAIN (auth, export, api, etc.)
    2. Create openspec/specs/{domain}/spec.md
    3. Write "# {Domain} Specification"
    4. Write "## Purpose" from intro paragraphs
    5. Write "## Requirements"
    6. FOR each feature/behavior described:
        a. Write "### Requirement: {Name}"
        b. Write "The system {SHALL|MUST|SHOULD} {behavior}."
        c. FOR each example, edge case, or condition:
            Write "#### Scenario: {Name}"
            Write "- GIVEN {precondition}"
            Write "- WHEN {trigger}"
            Write "- THEN {outcome}"
    7. DISCARD: implementation details, class names, internal architecture
       (those go in design.md if needed)
```

---

## 6. Conversion Rules — Markdown to Change Artifacts

If your Markdown describes a **planned change** rather than current behavior, convert it into a change folder:

| Source Content | Target Artifact | Guidance |
|---|---|---|
| Problem statement, motivation | `proposal.md` → `## Intent` | Why are we doing this? |
| Scope / boundaries | `proposal.md` → `## Scope` | In-scope and out-of-scope lists |
| New behaviors to add | `specs/{domain}/spec.md` → `## ADDED Requirements` | GIVEN/WHEN/THEN for each |
| Existing behaviors to change | `specs/{domain}/spec.md` → `## MODIFIED Requirements` | Note what it was previously |
| Behaviors to remove | `specs/{domain}/spec.md` → `## REMOVED Requirements` | Note reason for removal |
| Technical approach, architecture | `design.md` | Decisions, data flow, file changes |
| Todo lists, implementation steps | `tasks.md` | Numbered checkbox items |

---

## 7. Step-by-Step Conversion Walkthrough

### Example: Converting a Confluence-exported "User Authentication API" page

**Original Markdown:**

```markdown
---
title: "User Authentication API"
confluence_id: "12345"
space: "ENG"
author: "Jane Doe"
labels:
  - api
  - authentication
---

# User Authentication API

This service handles user login, token issuance, and session management.
Built with ASP.NET Core and uses JWT tokens.

## Login Endpoint

POST /api/auth/login

Accepts username and password. Returns a JWT token on success.
Returns 401 on invalid credentials.

| Field    | Type   | Description          |
|----------|--------|----------------------|
| username | string | User login (required)|
| password | string | Password (required)  |

## Get Current User

GET /api/auth/me

Returns the profile of the currently authenticated user.
Requires a valid Bearer token in the Authorization header.

## Session Expiration

Sessions expire after 24 hours of inactivity.
The client receives a 401 response when the token has expired.

## Rate Limiting

The login endpoint is rate-limited to 5 attempts per minute per IP.
After exceeding the limit, the client receives a 429 response.
```

### Step 1: Identify the domain

The domain is **auth** (authentication and session management).

### Step 2: Strip non-spec content

Remove: front-matter, implementation details ("Built with ASP.NET Core"), internal architecture.

### Step 3: Write the spec

**File: `openspec/specs/auth/spec.md`**

```markdown
# Auth Specification

## Purpose
Authentication, token issuance, and session management for application users.

## Requirements

### Requirement: User Login
The system SHALL accept POST requests to /api/auth/login with username and password credentials and return a JWT token on success.

#### Scenario: Valid credentials
- GIVEN a user with valid credentials
- WHEN the user submits a POST to /api/auth/login with username and password
- THEN a JWT token is returned
- AND the response status is 200

#### Scenario: Invalid credentials
- GIVEN invalid credentials
- WHEN the user submits a POST to /api/auth/login
- THEN a 401 Unauthorized response is returned
- AND no token is issued

### Requirement: Current User Retrieval
The system SHALL return the authenticated user's profile via GET /api/auth/me when a valid Bearer token is provided.

#### Scenario: Valid token
- GIVEN a user with a valid Bearer token
- WHEN the user sends GET /api/auth/me
- THEN the user's profile is returned

#### Scenario: Missing or expired token
- GIVEN no token or an expired token
- WHEN the user sends GET /api/auth/me
- THEN a 401 Unauthorized response is returned

### Requirement: Session Expiration
The system MUST expire sessions after 24 hours of inactivity.

#### Scenario: Idle timeout
- GIVEN an authenticated session
- WHEN 24 hours pass without activity
- THEN the session token is invalidated
- AND subsequent requests return 401

### Requirement: Login Rate Limiting
The system SHOULD rate-limit login attempts to 5 per minute per IP address.

#### Scenario: Rate limit exceeded
- GIVEN a client IP that has made 5 login attempts in the last minute
- WHEN a 6th login attempt is made
- THEN a 429 Too Many Requests response is returned
```

### Step 4: If proposing a change to this spec, create a change

```
openspec/changes/add-2fa/
├── proposal.md
├── design.md
├── tasks.md
└── specs/
    └── auth/
        └── spec.md        ← delta spec
```

**`proposal.md`:**
```markdown
# Proposal: Add Two-Factor Authentication

## Intent
Improve account security by requiring a second factor during login
for users who opt in.

## Scope
In scope:
- TOTP-based 2FA enrollment and verification
- QR code generation for authenticator apps
- Backup codes

Out of scope:
- SMS-based 2FA
- Hardware key support (future work)

## Approach
Add a 2FA step after password verification. Use TOTP with
standard authenticator app compatibility.
```

**`specs/auth/spec.md` (delta):**
```markdown
# Delta for Auth

## ADDED Requirements

### Requirement: Two-Factor Authentication
The system MUST support TOTP-based two-factor authentication for users who enable it.

#### Scenario: 2FA enrollment
- GIVEN a user without 2FA enabled
- WHEN the user enables 2FA in account settings
- THEN a QR code is displayed for authenticator app setup
- AND the user must verify with a code before activation

#### Scenario: 2FA login challenge
- GIVEN a user with 2FA enabled
- WHEN the user submits valid credentials
- THEN an OTP challenge is presented
- AND login completes only after a valid OTP is submitted

#### Scenario: Invalid OTP
- GIVEN a user with 2FA enabled who has entered valid credentials
- WHEN an invalid OTP is submitted
- THEN login is rejected
- AND the attempt counts toward rate limiting

## MODIFIED Requirements

### Requirement: Session Expiration
The system MUST expire sessions after 12 hours of inactivity.
(Previously: 24 hours)

#### Scenario: Idle timeout
- GIVEN an authenticated session
- WHEN 12 hours pass without activity
- THEN the session token is invalidated
```

**`tasks.md`:**
```markdown
# Tasks

## 1. TOTP Infrastructure
- [ ] 1.1 Add TOTP library dependency
- [ ] 1.2 Create TOTP secret generation and QR code endpoint
- [ ] 1.3 Create OTP verification endpoint

## 2. Enrollment Flow
- [ ] 2.1 Add 2FA settings page
- [ ] 2.2 Implement QR code display and verification step
- [ ] 2.3 Generate and store backup codes

## 3. Login Flow
- [ ] 3.1 Add OTP challenge step after password verification
- [ ] 3.2 Integrate with rate limiting
- [ ] 3.3 Update session expiration to 12 hours
```

---

## 8. Before & After Examples

### Example A: Feature description → Spec requirement

**Before (plain Markdown):**
```markdown
## File Export
The exporter downloads all pages from the configured Confluence spaces.
It paginates through results in batches of 100 and saves each page as a JSON file.
Gliffy diagram attachments can optionally be downloaded.
```

**After (OpenSpec):**
```markdown
### Requirement: Space Export
The system SHALL export all pages from configured Confluence spaces.

#### Scenario: Full space export
- GIVEN one or more space keys are configured
- WHEN the export is initiated
- THEN all pages in those spaces are retrieved
- AND each page is persisted as an individual JSON file

#### Scenario: Page limit
- GIVEN a configured page limit of N
- WHEN the export retrieves N pages
- THEN export stops and no additional pages are fetched

### Requirement: Gliffy Attachment Export
The system MAY download Gliffy diagram attachments for each page when enabled.

#### Scenario: Gliffy export enabled
- GIVEN ExportGliffySvg is true
- WHEN a page has .gliffy attachments
- THEN the attachments are downloaded and stored with the page data

#### Scenario: Gliffy export disabled
- GIVEN ExportGliffySvg is false
- WHEN a page has .gliffy attachments
- THEN the attachments are not downloaded
```

### Example B: Architecture notes → Design artifact

**Before:**
```markdown
## Architecture
The pipeline uses 5 stages. Stage 1 calls the Confluence REST API.
Stage 2 uses HtmlAgilityPack to parse XHTML. The system uses
Microsoft.Extensions.DependencyInjection for IoC.
```

**After (`design.md`):**
````markdown
# Design: Documentation Pipeline

## Technical Approach
Sequential 5-stage pipeline with dependency injection.
Each stage is a standalone service resolved from the DI container.

## Architecture Decisions

### Decision: Sequential Stages
Stages run sequentially (1→2→3→4→5) because each stage's
output is the next stage's input. No parallelism between stages.

### Decision: DI Container
Using Microsoft.Extensions.DependencyInjection for service resolution.
Each stage registered as Transient; config objects as Singletons.

## Data Flow
```
Confluence API → JSON files → Markdown files → Categorized files → OAS YAML → Azure
```
````

---

## 9. Checklist

Use this checklist when converting a Markdown file to OpenSpec:

```markdown
- [ ] Identified the domain(s) the document covers
- [ ] Created openspec/specs/{domain}/spec.md
- [ ] Wrote ## Purpose (1-2 sentences, no implementation details)
- [ ] Extracted each feature/behavior as a ### Requirement:
- [ ] Used RFC 2119 keywords (SHALL, MUST, SHOULD, MAY) for each requirement
- [ ] Added #### Scenario: blocks with GIVEN/WHEN/THEN for each requirement
- [ ] Included both happy-path and error/edge-case scenarios
- [ ] Removed implementation details (class names, libraries, internal architecture)
- [ ] Moved technical approach content to design.md (if in a change)
- [ ] Moved step-by-step procedures to tasks.md (if in a change)
- [ ] Removed YAML front-matter
- [ ] Validated: each requirement describes observable behavior, not internal mechanics
```

---

## 10. Quick Reference

### Spec file skeleton

```markdown
# {Domain} Specification

## Purpose
{What this domain covers.}

## Requirements

### Requirement: {Name}
The system {SHALL|MUST|SHOULD|MAY} {behavior}.

#### Scenario: {Name}
- GIVEN {precondition}
- WHEN {action}
- THEN {outcome}
```

### Change folder skeleton

```
openspec/changes/{change-name}/
├── proposal.md       # Intent, Scope, Approach
├── design.md         # Technical Approach, Decisions, Data Flow
├── tasks.md          # Numbered checkbox items
└── specs/
    └── {domain}/
        └── spec.md   # ADDED / MODIFIED / REMOVED Requirements
```

### RFC 2119 Keywords

| Keyword | Strength | Use when... |
|---|---|---|
| **MUST** / **SHALL** | Absolute | Non-negotiable requirement |
| **MUST NOT** / **SHALL NOT** | Absolute | Prohibited behavior |
| **SHOULD** | Recommended | Expected behavior with justified exceptions |
| **SHOULD NOT** | Discouraged | Avoid unless justified |
| **MAY** | Optional | Genuinely optional feature |

### OpenSpec CLI Quick Start

```bash
npm install -g @fission-ai/openspec@latest
cd your-project
openspec init
# Then tell your AI: /opsx:propose <what-you-want-to-build>
```
