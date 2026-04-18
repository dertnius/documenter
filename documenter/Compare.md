# OpenSpec vs Spec Kit — Comparison

## Philosophy at a Glance

| | **OpenSpec** | **Spec Kit** |
|---|---|---|
| **Core idea** | Living specs describe current system behavior; changes are proposed as deltas | Specs drive new feature development through a multi-phase refinement pipeline |
| **Orientation** | **System-centric** — "how does the system behave today?" | **Feature-centric** — "what are we building next?" |
| **Scope** | Entire system (specs/) + incremental changes (changes/) | Per-feature folders with self-contained artifacts |
| **AI integration** | Generic — works with any AI via `/opsx:` commands | Tight — designed for GitHub Copilot with `/speckit.` slash commands |
| **Artifact count** | 2 core (spec + change folder with 4 sub-artifacts) | 3 core (spec, plan, tasks) + 4 supporting (research, data-model, quickstart, contracts) |

---

## Advantages & Disadvantages

### OpenSpec

| Advantages | Disadvantages |
|---|---|
| **Simpler structure** — just `specs/` and `changes/`, flat and easy to navigate | **No separation of "what" vs "how"** — design.md exists but is loosely defined; easy to mix concerns |
| **Delta specs are powerful** — ADDED/MODIFIED/REMOVED sections make change impact explicit and auditable | **No built-in phased workflow** — you decide when to write proposal vs design vs tasks; no enforced order |
| **Domain-organized specs** — maps naturally to bounded contexts, microservices, or pipeline stages | **No constitution/governance layer** — no project-wide principles file; standards must live elsewhere |
| **Lightweight** — minimal ceremony, quick to adopt; `npm install` and go | **Weaker task format** — simple numbered checkboxes with no parallel markers, story tracing, or dependency modeling |
| **Change archival** — completed changes move to `archive/` with date prefix, creating a changelog | **No clarification phase** — ambiguities aren't systematically surfaced before planning |
| **RFC 2119 keywords** (SHALL/MUST/SHOULD/MAY) — industry-standard requirement language | **No data model or contract artifacts** — you're on your own for schemas and API specs |
| **Spec = source of truth** — specs always reflect current state, not aspirational state | **Harder for greenfield** — designed for systems that already exist and need documentation |

### Spec Kit

| Advantages | Disadvantages |
|---|---|
| **Strict phase separation** — spec (what/why) → plan (how) → tasks (steps) prevents mixing concerns | **Heavier ceremony** — 7 artifacts per feature (spec, plan, research, data-model, quickstart, contracts, tasks) |
| **Constitution** — project-wide principles that gate every plan via Constitution Check | **Feature-scoped only** — no concept of "current system state" specs; everything is per-feature |
| **Rich task format** — `[P]` parallel markers, `[US#]` story tracing, phased execution, checkpoints, dependency docs | **Steeper learning curve** — more templates to learn, more rules to follow |
| **Clarify phase** — explicitly surfaces ambiguities before planning begins | **Tightly coupled to Copilot** — slash commands assume GitHub Copilot; less portable to other AI tools |
| **User stories with priorities** — P1 = MVP, each story independently testable and deliverable | **No delta/change tracking** — no built-in mechanism to describe what changed relative to previous state |
| **Supporting artifacts** — research.md, data-model.md, quickstart.md, contracts/ are first-class | **No archival workflow** — no convention for completed features or changelog generation |
| **Incremental delivery** — task phases enable MVP-first delivery with story-by-story expansion | **Overkill for small changes** — a one-line bug fix still gets the full spec/plan/tasks treatment if you follow the process |

---

## Side-by-Side Format Comparison

### Requirements

| Aspect | OpenSpec | Spec Kit |
|---|---|---|
| **Keyword style** | RFC 2119: `SHALL`, `MUST`, `SHOULD`, `MAY` | Same keywords but in functional requirements: `FR-001: System MUST...` |
| **Scenarios** | `GIVEN` / `WHEN` / `THEN` (dash-prefixed list) | `**Given**` / `**When**` / `**Then**` (bold inline) |
| **Numbering** | No requirement IDs | `FR-001`, `SC-001` IDs for traceability |
| **Unknowns** | Not formalized | `[NEEDS CLARIFICATION: ...]` markers |

### Tasks

| Aspect | OpenSpec | Spec Kit |
|---|---|---|
| **Format** | `- [ ] 1.1 Description` | `- [ ] T001 [P] [US1] Description in src/path/file.cs` |
| **Grouping** | By logical group | By user story phase |
| **Parallelism** | Not marked | `[P]` marker for parallel-safe tasks |
| **Traceability** | None | `[US#]` links every task to a user story |
| **File paths** | Optional | Required in every task |
| **Checkpoints** | None | After every story phase |
| **Dependencies** | Not documented | Explicit dependency section at bottom |

### Architecture / Design

| Aspect | OpenSpec | Spec Kit |
|---|---|---|
| **Location** | `design.md` inside a change folder | `plan.md` inside a feature folder |
| **Tech context** | Freeform | Structured fields: Language, Dependencies, Storage, Testing, Platform, etc. |
| **Constitution gate** | None | `## Constitution Check` with pass/fail per principle |
| **Data model** | Inline in design.md | Separate `data-model.md` |
| **API contracts** | Inline or separate | Separate `contracts/` folder |

---

## Conversion Steps — Any Markdown File

### To OpenSpec (7 steps)

```
Step 1 │ IDENTIFY DOMAIN
       │ Read the markdown. What system area does it describe?
       │ Examples: auth, export, upload, search, pipeline
       │
Step 2 │ CREATE SPEC FILE
       │ Create: openspec/specs/{domain}/spec.md
       │
Step 3 │ WRITE PURPOSE
       │ Condense intro paragraphs → "## Purpose" (1-2 sentences)
       │ Strip: front-matter, author info, dates
       │
Step 4 │ EXTRACT REQUIREMENTS
       │ For each feature/behavior in the markdown:
       │   → "### Requirement: {Name}"
       │   → "The system {SHALL|MUST|SHOULD|MAY} {observable behavior}."
       │   Rule: if the implementation can change without visible effect,
       │         it doesn't belong here
       │
Step 5 │ WRITE SCENARIOS
       │ For each requirement, write 1+ scenarios:
       │   → "#### Scenario: {Name}"
       │   → "- GIVEN {precondition}"
       │   → "- WHEN {action}"
       │   → "- THEN {outcome}"
       │ Include happy path AND edge cases
       │
Step 6 │ MOVE TECHNICAL CONTENT
       │ Architecture, class names, library choices → design.md
       │ Step-by-step procedures → tasks.md
       │ (Only if proposing a change; otherwise discard)
       │
Step 7 │ VALIDATE
       │ □ Every requirement uses RFC 2119 keywords
       │ □ Every requirement has at least one scenario
       │ □ No implementation details in the spec
       │ □ Purpose section is implementation-free
```

### To Spec Kit (10 steps)

```
Step 1  │ DETERMINE FEATURE NAME
        │ Read the markdown. What feature does it describe?
        │ Create: .specify/specs/NNN-feature-name/
        │
Step 2  │ CREATE spec.md — HEADER
        │ Feature Branch, Created date, Status: Draft
        │ Input: condense the original requirement into one sentence
        │
Step 3  │ EXTRACT USER STORIES
        │ For each user journey in the markdown:
        │   → "### User Story N - {Title} (Priority: PN)"
        │   → Plain-language description
        │   → "**Why this priority**:" rationale
        │   → "**Independent Test**:" how to verify in isolation
        │ P1 = MVP, each story must deliver value alone
        │
Step 4  │ WRITE ACCEPTANCE SCENARIOS
        │ For each user story:
        │   → "**Given** X, **When** Y, **Then** Z"
        │ Include happy path + error cases
        │
Step 5  │ EXTRACT FUNCTIONAL REQUIREMENTS
        │ → "FR-001: System MUST {behavior}"
        │ Use MUST / SHOULD / MAY
        │ Flag unknowns: [NEEDS CLARIFICATION: ...]
        │
Step 6  │ WRITE SUCCESS CRITERIA + ASSUMPTIONS
        │ → "SC-001: {measurable metric}"
        │ → Assumptions as bullet list
        │ REMOVE all tech stack details from spec.md
        │
Step 7  │ CREATE plan.md — TECHNICAL CONTEXT
        │ Fill in: Language, Dependencies, Storage, Testing,
        │ Platform, Project Type, Performance Goals, Constraints
        │ Add project structure as file tree
        │ Run Constitution Check against .specify/memory/constitution.md
        │
Step 8  │ CREATE SUPPORTING ARTIFACTS (as needed)
        │ → research.md (library evaluations)
        │ → data-model.md (entities, relationships)
        │ → contracts/ (API specs, schemas)
        │ → quickstart.md (build/run/test instructions)
        │
Step 9  │ CREATE tasks.md — TASK BREAKDOWN
        │ Phase 1: Setup (project init)
        │ Phase 2: Foundational (blocking infrastructure)
        │ Phase 3+: One phase per user story (P1 first)
        │ Phase N: Polish (cross-cutting)
        │ Every task: T### [P?] [US#] description in src/path/file.ext
        │ Add checkpoints after each phase
        │ Add dependency section at bottom
        │
Step 10 │ VALIDATE
        │ □ spec.md has no tech stack or class names
        │ □ plan.md has no user stories or requirements
        │ □ tasks.md has file paths, [US#] labels, checkpoints
        │ □ Each user story is independently testable
        │ □ Unknowns flagged with [NEEDS CLARIFICATION]
```

---

## Which Works Better?

| Scenario | Winner | Why |
|---|---|---|
| **Documenting an existing system** | **OpenSpec** | Domain-organized specs describe current behavior; delta specs track evolution |
| **Building a new feature from scratch** | **Spec Kit** | Multi-phase pipeline (specify → clarify → plan → tasks) prevents jumping to code too early |
| **Small/quick changes** | **OpenSpec** | Lighter ceremony; a proposal + delta spec is enough |
| **Complex multi-story features** | **Spec Kit** | User story prioritization, phased tasks, parallel markers, and checkpoints manage complexity |
| **Team with mixed AI tools** | **OpenSpec** | Tool-agnostic; works with any AI assistant |
| **Team using GitHub Copilot** | **Spec Kit** | Native slash commands, designed for Copilot's workflow |
| **Maintaining a living spec over time** | **OpenSpec** | Specs are the source of truth; deltas merge back; archive creates history |
| **Governance / compliance** | **Spec Kit** | Constitution + Constitution Check gates enforce project-wide standards |
| **Onboarding new developers** | **Spec Kit** | quickstart.md, research.md, and structured plans make context explicit |
| **API-heavy projects** | **Tie** | OpenSpec has scenarios per endpoint; Spec Kit has dedicated contracts/ folder |

### Summary Recommendation

| If you need... | Use... |
|---|---|
| A **source of truth** for how your system works today, with tracked changes over time | **OpenSpec** |
| A **feature development pipeline** that goes from idea → spec → plan → tasks → code | **Spec Kit** |
| **Both** (document current state AND drive new features) | Use **OpenSpec** for system specs + **Spec Kit** for new feature development — they don't conflict |

The two approaches are complementary. OpenSpec answers "what does the system do now?" while Spec Kit answers "what are we building next and how?" For a project like the documenter pipeline — which already exists and may get new features — using OpenSpec to capture the current 5-stage pipeline behavior and Spec Kit to plan new features (e.g., adding a new export format) would give you the best of both.