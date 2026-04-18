# Spec-Driven Development 101

> How to adopt OpenSpec and Spec Kit for legacy rescue, greenfield MVPs, and polyglot cloud-native applications with heavy API and database workloads.

---

## Table of Contents

1. [The Problem](#1-the-problem)
2. [Decision Matrix — OpenSpec vs Spec Kit vs Both](#2-decision-matrix)
3. [Documentation Tiers](#3-documentation-tiers)
4. [Phase 0: Triage — Assessing What You Have](#4-phase-0-triage)
5. [Phase 1: Legacy Rescue with OpenSpec](#5-phase-1-legacy-rescue-with-openspec)
6. [Phase 2: Greenfield MVP with Spec Kit](#6-phase-2-greenfield-mvp-with-spec-kit)
7. [Phase 3: Running Both Together](#7-phase-3-running-both-together)
8. [Per-Stack Playbooks](#8-per-stack-playbooks)
9. [Cloud-Native & App Service Considerations](#9-cloud-native--app-service-considerations)
10. [Heavy API Patterns](#10-heavy-api-patterns)
11. [Heavy Database Patterns](#11-heavy-database-patterns)
12. [Quick-Start Recipes](#12-quick-start-recipes)

---

## 1. The Problem

You have some combination of:

```
┌─────────────────────────────────────────────────────────┐
│  LEGACY CODE          │  GREENFIELD MVP                 │
│  ─────────────        │  ──────────────                 │
│  • Exists today       │  • Doesn't exist yet            │
│  • Poor/no docs       │  • Needs specs BEFORE code      │
│  • "Tribal knowledge" │  • Multiple API stacks          │
│  • Nobody knows why   │  • Heavy DB, cloud-native       │
│  • Afraid to touch it │  • Ship fast, don't break later │
│                       │                                 │
│  NEED: Understand     │  NEED: Plan before building     │
│  what it does NOW     │  what it does NEXT              │
└─────────────────────────────────────────────────────────┘
```

**OpenSpec** solves the left column. **Spec Kit** solves the right column. For most real projects, you need both.

---

## 2. Decision Matrix

| Your Situation | Use | Why |
|---|---|---|
| Legacy code, no docs, need to understand it | **OpenSpec only** | Capture current behavior as specs |
| Pure greenfield, no existing code | **Spec Kit only** | Drive development from specs |
| Legacy + building new features on top | **Both** | OpenSpec for current state, Spec Kit for new features |
| Multiple API stacks (Java + .NET + Python) | **Both** | OpenSpec per service domain, Spec Kit per new feature |
| Heavy API surface | **Both** | OpenSpec specs per endpoint domain, Spec Kit contracts/ for new APIs |
| Heavy database | **Both** | OpenSpec for current schema behavior, Spec Kit data-model.md for new entities |
| Cloud-native / App Service | **Both** | OpenSpec for infra behavior, Spec Kit for deployment plans |
| MVP with deadline pressure | **Spec Kit first** | Plan → tasks → implement; add OpenSpec after launch |

### The Recommendation for Your Case

```
Legacy + Greenfield + Heavy API + Heavy DB + Polyglot + Cloud-Native + MVP
                                    │
                                    ▼
                              USE BOTH
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
              OpenSpec                          Spec Kit
         (existing systems)                 (new features)
                    │                               │
         Capture current API              Spec → Plan → Tasks
         behavior per domain              per MVP feature
         across Java/.NET/Python          with constitution
```

---

## 3. Documentation Tiers

Not every piece of code needs the same depth. Use tiers to avoid over-documenting low-risk code and under-documenting critical paths.

### Tier 1: Minimum Viable Documentation (Quick Capture)

**When**: Legacy code you need to understand but won't change soon.

```
openspec/specs/{domain}/spec.md     ← Requirements + scenarios only
```

**Effort**: 30 min per domain. Read the code, write what it does.

**Example**:
```markdown
# Payment Processing Specification

## Purpose
Processes credit card payments via Stripe and records transactions.

## Requirements

### Requirement: Charge Customer
The system SHALL submit a charge to Stripe when a valid order is placed.

#### Scenario: Successful charge
- GIVEN a valid order with payment details
- WHEN the order is submitted
- THEN a Stripe charge is created
- AND the transaction is recorded with status "completed"

#### Scenario: Declined card
- GIVEN an order with a declined card
- WHEN the charge is attempted
- THEN the transaction is recorded with status "failed"
- AND the user receives an error message
```

That's it. No design doc, no tasks, no plan. Just capture the behavior.

---

### Tier 2: Standard Documentation (Feature-Ready)

**When**: You're about to build a new feature or significantly modify existing behavior.

```
.specify/specs/NNN-feature/
├── spec.md              ← User stories + requirements
├── plan.md              ← Tech stack + architecture
└── tasks.md             ← Implementation steps

openspec/specs/{domain}/
└── spec.md              ← Current behavior (if modifying existing)
```

**Effort**: 2-4 hours per feature.

---

### Tier 3: Full Documentation (Critical Path)

**When**: Core business logic, public APIs, data migrations, security-sensitive areas.

```
.specify/
├── memory/constitution.md                ← Project-wide standards
└── specs/NNN-feature/
    ├── spec.md                           ← User stories + requirements
    ├── plan.md                           ← Tech stack + architecture
    ├── research.md                       ← Library/framework evaluation
    ├── data-model.md                     ← Entity relationships + schema
    ├── quickstart.md                     ← Build/run/test locally
    ├── contracts/
    │   ├── api-spec.yaml                 ← OpenAPI 3.x
    │   └── db-migrations.md              ← Schema change plan
    └── tasks.md                          ← Full phased breakdown

openspec/
├── specs/{domain}/spec.md                ← Current behavior
└── changes/{change-name}/
    ├── proposal.md                       ← Why + scope
    ├── design.md                         ← Technical approach
    ├── tasks.md                          ← Implementation steps
    └── specs/{domain}/spec.md            ← Delta (ADDED/MODIFIED/REMOVED)
```

**Effort**: 1-2 days per critical feature.

---

### Tier Decision Guide

```
Is this code critical to revenue, security, or data integrity?
├── YES → Tier 3 (Full)
└── NO
    ├── Are you about to build or change it?
    │   ├── YES → Tier 2 (Standard)
    │   └── NO  → Tier 1 (Minimum)
    └── Is it a public API consumed by external clients?
        ├── YES → Tier 3 (Full)
        └── NO  → Tier 1 or 2
```

---

## 4. Phase 0: Triage — Assessing What You Have

Before writing any spec, inventory your codebase.

### Step 0.1: Map the Landscape

```
FOR each service/project in your system:
    Record:
    ├── Name: payment-api
    ├── Stack: Java 17 / Spring Boot
    ├── Type: REST API
    ├── Database: PostgreSQL
    ├── Docs exist: README only
    ├── API docs: Swagger auto-gen (outdated)
    ├── Tests: 12% coverage
    ├── Status: Production, active
    ├── Risk: HIGH (processes payments)
    └── Tier: 3 (critical path)
```

### Step 0.2: Create a Service Inventory

```markdown
| Service | Stack | DB | Docs Quality | Risk | Tier |
|---|---|---|---|---|---|
| payment-api | Java 17 / Spring Boot | PostgreSQL | Swagger (stale) | HIGH | 3 |
| user-api | .NET 8 / ASP.NET Core | SQL Server | README only | MEDIUM | 2 |
| analytics-worker | Python 3.11 / FastAPI | MongoDB | None | LOW | 1 |
| notification-svc | .NET 8 / Worker Service | Redis | None | MEDIUM | 2 |
| gateway | Java 17 / Spring Cloud Gateway | — | Config files | HIGH | 3 |
```

### Step 0.3: Prioritize

```
1. HIGH risk + NO docs     → Spec FIRST (Tier 3 OpenSpec)
2. HIGH risk + SOME docs   → Validate + upgrade docs (Tier 2-3)
3. Building new features   → Spec Kit for the feature (Tier 2+)
4. LOW risk + stable       → Tier 1 when convenient
```

---

## 5. Phase 1: Legacy Rescue with OpenSpec

### Goal: Capture what your existing systems do today.

### Step 1.1: Initialize OpenSpec

```bash
npm install -g @fission-ai/openspec@latest
cd your-monorepo      # or each service repo
openspec init
```

This creates:
```
openspec/
├── config.yaml
├── specs/
└── changes/
```

### Step 1.2: Identify Domains

Map your services to domains:

```
payment-api     → openspec/specs/payments/
user-api        → openspec/specs/users/
analytics       → openspec/specs/analytics/
notifications   → openspec/specs/notifications/
gateway         → openspec/specs/gateway/
```

For polyglot systems, domains map to **business capabilities**, not tech stacks.

### Step 1.3: Read Code → Write Specs

For each domain, read the existing code and extract behavior:

```
┌──────────────────────────────────────────────────────────────┐
│ SOURCE                          │ EXTRACT INTO SPEC          │
│─────────────────────────────────│────────────────────────────│
│ Controller/endpoint annotations │ Requirements (what the API │
│ (e.g., @GetMapping, [HttpPost]) │ accepts and returns)       │
│                                 │                            │
│ Service layer business logic    │ Requirements (what the     │
│                                 │ system does with the data) │
│                                 │                            │
│ Repository/DAO queries          │ Requirements (what data    │
│                                 │ is read/written)           │
│                                 │                            │
│ Exception handlers / filters    │ Scenarios (error cases)    │
│                                 │                            │
│ Config files / env vars         │ Requirements (configurable │
│                                 │ behaviors + defaults)      │
│                                 │                            │
│ Existing tests (if any)         │ Scenarios (the test IS     │
│                                 │ the Given/When/Then)       │
│                                 │                            │
│ Database migrations / schemas   │ Requirements (data         │
│                                 │ constraints, relationships)│
└──────────────────────────────────────────────────────────────┘
```

### Step 1.4: Write the Spec

```markdown
# Payments Specification

## Purpose
Processes credit card payments, handles refunds, and maintains transaction history.

## Requirements

### Requirement: Create Payment
The system SHALL accept POST /api/payments with order details and charge the customer's card.

#### Scenario: Successful payment
- GIVEN a valid order with amount > 0 and valid card token
- WHEN POST /api/payments is submitted
- THEN a charge is created via the payment provider
- AND a transaction record is persisted with status "completed"
- AND the response contains the transaction ID with status 201

#### Scenario: Insufficient funds
- GIVEN a valid order but the card has insufficient funds
- WHEN POST /api/payments is submitted
- THEN no charge is created
- AND a transaction record is persisted with status "failed"
- AND the response contains error details with status 402

#### Scenario: Invalid request
- GIVEN a request missing required fields (amount or card token)
- WHEN POST /api/payments is submitted
- THEN a 400 Bad Request response is returned
- AND no transaction record is created

### Requirement: Refund Payment
The system SHALL accept POST /api/payments/{id}/refund and reverse the original charge.

#### Scenario: Full refund
- GIVEN a completed transaction
- WHEN POST /api/payments/{id}/refund is submitted
- THEN the original charge is reversed via the payment provider
- AND the transaction status is updated to "refunded"

#### Scenario: Already refunded
- GIVEN a transaction with status "refunded"
- WHEN POST /api/payments/{id}/refund is submitted
- THEN a 409 Conflict response is returned

### Requirement: Transaction History
The system SHALL return paginated transaction history via GET /api/payments.

#### Scenario: Default pagination
- GIVEN transactions exist
- WHEN GET /api/payments is called without pagination params
- THEN the first 20 transactions are returned ordered by date descending

### Requirement: Payment Provider Configuration
The system MUST read the payment provider API key from environment variable PAYMENT_PROVIDER_KEY.

#### Scenario: Missing API key
- GIVEN PAYMENT_PROVIDER_KEY is not set
- WHEN the service starts
- THEN startup fails with a configuration error
```

### Step 1.5: Repeat for Each Domain

Work through your inventory in risk order. Spend:
- **30 min** per Tier 1 domain (capture the basics)
- **2 hours** per Tier 2 domain (thorough spec)
- **4+ hours** per Tier 3 domain (full spec with edge cases)

---

## 6. Phase 2: Greenfield MVP with Spec Kit

### Goal: Plan new features before writing code.

### Step 2.1: Initialize Spec Kit

```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@latest
cd your-project
specify init . --ai copilot
specify check
```

This creates:
```
.specify/
├── memory/
│   └── constitution.md
├── scripts/
├── specs/
└── templates/
```

### Step 2.2: Write the Constitution

The constitution applies to ALL features, ALL stacks. For a polyglot cloud-native project:

```markdown
# Project Constitution

## API Standards
- All APIs MUST follow REST conventions
- All endpoints MUST return consistent error response format:
  { "error": { "code": "string", "message": "string", "details": [] } }
- All APIs MUST have OpenAPI 3.x specs before implementation
- All breaking changes MUST be versioned (v1/, v2/)
- All APIs MUST validate input at the controller/handler level

## Database Standards
- All schema changes MUST use versioned migrations (never manual DDL)
- All tables MUST have created_at and updated_at timestamps
- All foreign keys MUST have explicit ON DELETE behavior
- No ORM-generated queries in production without review
- All queries touching > 1000 rows MUST be paginated

## Cross-Stack Standards
- Java services: Spring Boot 3.x, Java 17+
- .NET services: .NET 8+, ASP.NET Core minimal APIs or controllers
- Python services: Python 3.11+, FastAPI or Flask
- All services MUST use structured JSON logging
- All services MUST expose /health and /ready endpoints
- All secrets MUST come from environment variables or Azure Key Vault

## Cloud-Native Standards
- All services MUST be containerized (Docker)
- All services MUST be deployable to Azure App Service
- All services MUST handle graceful shutdown (SIGTERM)
- No local file system dependencies (use blob storage)
- All service-to-service calls MUST use retry with exponential backoff

## Testing Standards
- All public API endpoints MUST have integration tests
- All database migrations MUST be tested (up AND down)
- All services MUST pass health check within 30 seconds of startup

## Security
- All endpoints (except /health, /ready) MUST require authentication
- All user input MUST be sanitized before database operations
- No credentials in source code, config files, or logs
```

### Step 2.3: Specify the Feature

Run `/speckit.specify` or write manually.

For an MVP, identify the **minimum set of user stories** that deliver value:

```markdown
# Feature Specification: Order Management API

**Feature Branch**: `001-order-management`
**Created**: 2026-04-18
**Status**: Draft
**Input**: "Build an order management system with Java API for order processing,
.NET API for customer management, and Python API for analytics"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Place an Order (Priority: P1)

A customer selects products and submits an order. The system validates
inventory, calculates the total, and creates the order. The customer
receives an order confirmation with a tracking ID.

**Why this priority**: Core transaction — nothing else matters if orders can't be placed.

**Independent Test**: POST an order with valid items and verify it returns
a confirmation with status "pending".

**Acceptance Scenarios**:

1. **Given** a customer with items in cart and sufficient inventory, **When** POST /api/orders is submitted, **Then** the order is created with status "pending" and a tracking ID is returned
2. **Given** an item with zero inventory, **When** an order including that item is submitted, **Then** the order is rejected with a 409 Conflict listing the unavailable items
3. **Given** an empty cart, **When** POST /api/orders is submitted, **Then** a 400 Bad Request is returned

---

### User Story 2 - View Order Status (Priority: P2)

A customer checks the status of their order using the tracking ID.

**Why this priority**: Customers need visibility after placing an order.

**Independent Test**: Create an order, then GET /api/orders/{id} and verify it returns the order details.

**Acceptance Scenarios**:

1. **Given** a valid order ID, **When** GET /api/orders/{id} is called, **Then** the order details including status, items, and total are returned
2. **Given** an invalid order ID, **When** GET /api/orders/{id} is called, **Then** a 404 Not Found is returned

---

### User Story 3 - Customer Lookup (Priority: P2)

An operator looks up a customer's profile and order history.

**Why this priority**: Support operations need customer context.

**Independent Test**: GET /api/customers/{id}/orders returns the customer's order list.

**Acceptance Scenarios**:

1. **Given** a valid customer ID with orders, **When** GET /api/customers/{id}/orders is called, **Then** paginated order history is returned
2. **Given** a valid customer ID with no orders, **When** GET /api/customers/{id}/orders is called, **Then** an empty list is returned

---

### User Story 4 - Order Analytics Dashboard (Priority: P3)

A business analyst views order volume, revenue, and trends.

**Why this priority**: Business intelligence — valuable but not blocking core operations.

**Independent Test**: GET /api/analytics/orders?period=7d returns aggregated metrics.

**Acceptance Scenarios**:

1. **Given** orders exist in the last 7 days, **When** GET /api/analytics/orders?period=7d is called, **Then** aggregated metrics (count, revenue, avg order value) are returned

---

### Edge Cases

- What happens when two orders try to claim the last item in inventory simultaneously?
- How does the system handle partial order failures (some items available, others not)?
- What happens when the analytics API is down — does it affect order placement? [NEEDS CLARIFICATION: service isolation strategy]

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept orders via POST /api/orders (Java API)
- **FR-002**: System MUST validate inventory before confirming an order
- **FR-003**: System MUST generate a unique tracking ID for each order
- **FR-004**: System MUST expose order details via GET /api/orders/{id} (Java API)
- **FR-005**: System MUST expose customer profiles via GET /api/customers/{id} (.NET API)
- **FR-006**: System MUST expose customer order history via GET /api/customers/{id}/orders (.NET API)
- **FR-007**: System SHOULD expose order analytics via GET /api/analytics/orders (Python API)
- **FR-008**: System MUST paginate all list endpoints (default 20, max 100)
- **FR-009**: System MUST use consistent error response format across all APIs

### Key Entities

- **Order**: Tracking ID, customer ID, items (product + quantity + price), total, status, timestamps
- **Customer**: ID, name, email, address, created_at
- **Product**: ID, name, price, inventory count
- **OrderItem**: Order ID, product ID, quantity, unit price
- **AnalyticsSnapshot**: Period, order count, revenue, average order value

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Orders are created within 500ms at p95
- **SC-002**: Order lookup responds within 200ms at p95
- **SC-003**: Analytics queries respond within 2 seconds for 90-day windows
- **SC-004**: System handles 100 concurrent order submissions without errors

## Assumptions

- Authentication is handled by an API gateway (not in scope for individual services)
- Services communicate via REST (no event bus for MVP)
- Single-region deployment for MVP
- PostgreSQL for Java order API, SQL Server for .NET customer API, MongoDB for Python analytics
```

### Step 2.4: Clarify

Run `/speckit.clarify` or manually review the spec for `[NEEDS CLARIFICATION]` markers. Resolve each one before moving to the plan.

### Step 2.5: Plan

Create `plan.md` with the technical architecture:

```markdown
# Implementation Plan: Order Management

**Branch**: `001-order-management` | **Date**: 2026-04-18
**Input**: spec.md

## Summary

Three-service architecture: Java order API, .NET customer API, Python analytics API.
All deployed as Azure App Service containers behind an API gateway.

## Technical Context

**Services**:

| Service | Stack | Database | Port |
|---|---|---|---|
| order-api | Java 17 / Spring Boot 3.2 | PostgreSQL 15 | 8080 |
| customer-api | .NET 8 / ASP.NET Core | SQL Server 2022 | 5000 |
| analytics-api | Python 3.11 / FastAPI | MongoDB 7 | 8000 |

**Shared Infrastructure**:
- Azure API Management (gateway, auth, rate limiting)
- Azure Container Registry (image storage)
- Azure App Service (compute)
- Azure Key Vault (secrets)
- Azure Monitor (logging, metrics)

**Testing**: JUnit 5 (Java), xUnit (.NET), pytest (Python)

## Constitution Check

- [✅] REST conventions: All services follow REST
- [✅] Error format: Shared error schema in all APIs
- [✅] OpenAPI specs: contracts/ folder with specs per service
- [✅] Migrations: Flyway (Java), EF Core (C#), mongomigrate (Python)
- [✅] Health endpoints: /health and /ready on all services
- [✅] Containerized: Dockerfile per service
- [✅] Secrets: All via Azure Key Vault + env vars
- [✅] Structured logging: JSON to stdout, Azure Monitor collection
- [✅] Auth: API Management handles JWT validation

## Project Structure

```text
monorepo/
├── services/
│   ├── order-api/                          # Java / Spring Boot
│   │   ├── src/main/java/com/app/orders/
│   │   │   ├── controller/
│   │   │   │   └── OrderController.java
│   │   │   ├── model/
│   │   │   │   ├── Order.java
│   │   │   │   ├── OrderItem.java
│   │   │   │   └── Product.java
│   │   │   ├── repository/
│   │   │   │   ├── OrderRepository.java
│   │   │   │   └── ProductRepository.java
│   │   │   ├── service/
│   │   │   │   └── OrderService.java
│   │   │   └── config/
│   │   │       └── AppConfig.java
│   │   ├── src/main/resources/
│   │   │   ├── application.yml
│   │   │   └── db/migration/
│   │   ├── src/test/
│   │   ├── Dockerfile
│   │   └── pom.xml
│   │
│   ├── customer-api/                       # .NET 8 / ASP.NET Core
│   │   ├── Controllers/
│   │   │   └── CustomerController.cs
│   │   ├── Models/
│   │   │   └── Customer.cs
│   │   ├── Services/
│   │   │   └── CustomerService.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   └── customer-api.csproj
│   │
│   └── analytics-api/                      # Python 3.11 / FastAPI
│       ├── app/
│       │   ├── main.py
│       │   ├── routes/
│       │   │   └── analytics.py
│       │   ├── models/
│       │   │   └── schemas.py
│       │   ├── services/
│       │   │   └── analytics_service.py
│       │   └── config.py
│       ├── tests/
│       ├── Dockerfile
│       └── pyproject.toml
│
├── infrastructure/
│   ├── docker-compose.yml
│   ├── azure/
│   │   ├── app-service.bicep
│   │   └── api-management.bicep
│   └── gateway/
│       └── api-policy.xml
│
├── contracts/                              # Shared API contracts
│   ├── order-api.yaml                      # OpenAPI 3.x
│   ├── customer-api.yaml
│   ├── analytics-api.yaml
│   └── shared-schemas.yaml                 # Reusable error format, pagination
│
├── .specify/                               # Spec Kit artifacts
└── openspec/                               # OpenSpec current-state specs
```
```

### Step 2.6: Generate Tasks

Create `tasks.md` with phased breakdown (abbreviated — full version would include all tasks):

```markdown
# Tasks: Order Management

## Phase 1: Setup

- [ ] T001 Create monorepo structure with services/, contracts/, infrastructure/
- [ ] T002 [P] Initialize Java project: Spring Boot 3.2, Java 17, pom.xml
- [ ] T003 [P] Initialize .NET project: dotnet new webapi, .NET 8
- [ ] T004 [P] Initialize Python project: FastAPI, pyproject.toml
- [ ] T005 [P] Create Dockerfiles for all three services
- [ ] T006 Create docker-compose.yml with all services + databases
- [ ] T007 [P] Create shared-schemas.yaml (error format, pagination)

---

## Phase 2: Foundational

- [ ] T008 [P] Java: Configure PostgreSQL connection + Flyway in application.yml
- [ ] T009 [P] .NET: Configure SQL Server + EF Core in Program.cs
- [ ] T010 [P] Python: Configure MongoDB connection in app/config.py
- [ ] T011 [P] Java: Create Order, OrderItem, Product JPA entities
- [ ] T012 [P] .NET: Create Customer EF Core entity + migration
- [ ] T013 [P] Python: Create Pydantic schemas in app/models/schemas.py
- [ ] T014 [P] All: Implement /health and /ready endpoints
- [ ] T015 [P] All: Configure structured JSON logging

**Checkpoint**: All services start, connect to their databases, pass health checks

---

## Phase 3: User Story 1 - Place an Order (P1) 🎯 MVP

- [ ] T016 [US1] Java: Create OrderRepository in repository/OrderRepository.java
- [ ] T017 [US1] Java: Create ProductRepository in repository/ProductRepository.java
- [ ] T018 [US1] Java: Implement OrderService.createOrder() with inventory validation
- [ ] T019 [US1] Java: Implement POST /api/orders in controller/OrderController.java
- [ ] T020 [US1] Java: Add input validation (Bean Validation annotations)
- [ ] T021 [US1] Java: Create Flyway migration V1__create_orders_tables.sql
- [ ] T022 [P] [US1] Java: Write integration tests for POST /api/orders
- [ ] T023 [US1] Update contracts/order-api.yaml with POST /api/orders spec

**Checkpoint**: Can place orders via POST /api/orders with inventory validation

---

## Phase 4: User Story 2 - View Order Status (P2)

- [ ] T024 [US2] Java: Implement GET /api/orders/{id} in OrderController.java
- [ ] T025 [P] [US2] Java: Write integration tests for GET /api/orders/{id}
- [ ] T026 [US2] Update contracts/order-api.yaml with GET /api/orders/{id}

**Checkpoint**: Can view order status by ID

---

## Phase 5: User Story 3 - Customer Lookup (P2)

- [ ] T027 [US3] .NET: Implement CustomerService in Services/CustomerService.cs
- [ ] T028 [US3] .NET: Implement GET /api/customers/{id} in CustomerController.cs
- [ ] T029 [US3] .NET: Implement GET /api/customers/{id}/orders (calls order-api)
- [ ] T030 [US3] .NET: Add EF Core migration for Customers table
- [ ] T031 [P] [US3] .NET: Write integration tests
- [ ] T032 [US3] Update contracts/customer-api.yaml

**Checkpoint**: Can look up customers and their order history

---

## Phase 6: User Story 4 - Analytics Dashboard (P3)

- [ ] T033 [US4] Python: Create analytics_service.py with aggregation queries
- [ ] T034 [US4] Python: Implement GET /api/analytics/orders in routes/analytics.py
- [ ] T035 [US4] Python: Create MongoDB indexes for time-range queries
- [ ] T036 [P] [US4] Python: Write pytest tests
- [ ] T037 [US4] Update contracts/analytics-api.yaml

**Checkpoint**: Analytics endpoint returns metrics for configurable periods

---

## Phase 7: Polish

- [ ] T038 [P] Azure Bicep templates for App Service deployment
- [ ] T039 [P] API Management gateway configuration
- [ ] T040 [P] CI/CD pipeline (build + test + deploy per service)
- [ ] T041 Cross-service integration test (order → customer lookup → analytics)

## Dependencies

- Phase 1 (Setup): No dependencies
- Phase 2 (Foundational): Depends on Phase 1
- Phase 3 (Orders / US1): Depends on Phase 2 — Java stack only
- Phase 4 (Order Status / US2): Depends on Phase 3
- Phase 5 (Customer / US3): Depends on Phase 2 — .NET stack only, can parallel with Phase 3-4
- Phase 6 (Analytics / US4): Depends on Phase 2 — Python stack only, can parallel with Phase 3-5
- Phase 7 (Polish): After Phase 3 minimum (MVP), ideally after all
```

### Step 2.7: Implement

Execute tasks in order. After each phase checkpoint, validate before proceeding.

---

## 7. Phase 3: Running Both Together

### Directory Layout

```
your-project/
├── .specify/                          # Spec Kit (new features)
│   ├── memory/
│   │   └── constitution.md
│   └── specs/
│       ├── 001-order-management/
│       │   ├── spec.md
│       │   ├── plan.md
│       │   ├── data-model.md
│       │   ├── contracts/
│       │   └── tasks.md
│       └── 002-add-notifications/
│           └── ...
│
├── openspec/                          # OpenSpec (current state)
│   ├── specs/
│   │   ├── orders/
│   │   │   └── spec.md               # "What does order-api do TODAY?"
│   │   ├── customers/
│   │   │   └── spec.md               # "What does customer-api do TODAY?"
│   │   ├── analytics/
│   │   │   └── spec.md
│   │   └── infrastructure/
│   │       └── spec.md               # Deployment, networking, scaling
│   └── changes/
│       └── add-webhooks/
│           ├── proposal.md
│           └── specs/orders/spec.md   # Delta: ADDED webhook requirements
│
├── services/
│   ├── order-api/
│   ├── customer-api/
│   └── analytics-api/
└── contracts/
```

### Workflow When Adding a Feature

```
1. CHECK openspec/specs/        → What does the system do now?
2. RUN /speckit.specify         → What do we want it to do?
3. RUN /speckit.clarify         → What's ambiguous?
4. RUN /speckit.plan            → How will we build it?
5. RUN /speckit.tasks           → What are the steps?
6. RUN /speckit.implement       → Build it
7. UPDATE openspec/specs/       → Merge new behavior into current-state specs
```

Step 7 is critical — after implementation, the OpenSpec specs must be updated to reflect the new reality. You can use OpenSpec's change/archive workflow for this, or directly update the spec files.

---

## 8. Per-Stack Playbooks

### Java API (Spring Boot)

```
WHERE TO LOOK FOR EXISTING BEHAVIOR:
├── @RestController classes          → API endpoints → Requirements
├── @Service classes                 → Business logic → Requirements
├── @Repository / JPA entities       → Data behavior → Requirements
├── application.yml                  → Configuration → Requirements (defaults)
├── @ExceptionHandler               → Error handling → Scenarios
├── src/test/                        → Existing tests → Scenarios (copy the Given/When/Then)
├── db/migration/ (Flyway)          → Schema → data-model.md
└── pom.xml                         → Dependencies → plan.md Technical Context

SPEC KIT PLAN TEMPLATE:
  Language/Version: Java 17
  Primary Dependencies: Spring Boot 3.2, Spring Data JPA, Flyway
  Storage: PostgreSQL 15
  Testing: JUnit 5, MockMvc, Testcontainers
  Project Type: web-service
```

### .NET API (ASP.NET Core)

```
WHERE TO LOOK FOR EXISTING BEHAVIOR:
├── Controllers/                     → API endpoints → Requirements
├── Services/                        → Business logic → Requirements
├── Models/ or Entities/             → Data → Requirements
├── Data/AppDbContext.cs             → EF Core config → data-model.md
├── Data/Migrations/                 → Schema history → data-model.md
├── Program.cs                       → DI, middleware → plan.md
├── appsettings.json                 → Config defaults → Requirements
├── *.csproj                         → Dependencies → plan.md
└── Tests/                           → Test cases → Scenarios

SPEC KIT PLAN TEMPLATE:
  Language/Version: C# / .NET 8
  Primary Dependencies: ASP.NET Core, Entity Framework Core
  Storage: SQL Server 2022
  Testing: xUnit, WebApplicationFactory
  Project Type: web-service
```

### Python API (FastAPI)

```
WHERE TO LOOK FOR EXISTING BEHAVIOR:
├── routes/ or routers/              → API endpoints → Requirements
├── services/                        → Business logic → Requirements
├── models/ or schemas/              → Pydantic models → Requirements + data-model.md
├── config.py                        → Configuration → Requirements
├── main.py                          → App setup, middleware → plan.md
├── pyproject.toml / requirements.txt→ Dependencies → plan.md
├── tests/                           → Test cases → Scenarios
└── alembic/ or migrations/          → Schema → data-model.md

SPEC KIT PLAN TEMPLATE:
  Language/Version: Python 3.11
  Primary Dependencies: FastAPI, SQLAlchemy or Motor (MongoDB)
  Storage: MongoDB 7 (or PostgreSQL)
  Testing: pytest, httpx (async test client)
  Project Type: web-service
```

---

## 9. Cloud-Native & App Service Considerations

### What to Spec in OpenSpec

Create `openspec/specs/infrastructure/spec.md`:

```markdown
# Infrastructure Specification

## Purpose
Deployment, scaling, networking, and operational behavior of all services.

## Requirements

### Requirement: Container Deployment
The system SHALL deploy all services as Docker containers on Azure App Service.

#### Scenario: Service startup
- GIVEN a service container is deployed
- WHEN the container starts
- THEN the /health endpoint responds with 200 within 30 seconds
- AND the /ready endpoint responds with 200 when all dependencies are connected

### Requirement: Secrets Management
The system MUST read all secrets from Azure Key Vault via environment variables.

#### Scenario: Missing secret
- GIVEN a required secret is not available in Key Vault
- WHEN the service starts
- THEN startup fails with a descriptive error (without logging the secret name or value)

### Requirement: Graceful Shutdown
The system MUST handle SIGTERM gracefully.

#### Scenario: Shutdown during request processing
- GIVEN active requests are being processed
- WHEN SIGTERM is received
- THEN new requests are rejected
- AND in-flight requests are allowed to complete (up to 30 second timeout)
- AND the process exits with code 0

### Requirement: Scaling
The system SHOULD auto-scale based on CPU utilization.

#### Scenario: High load
- GIVEN CPU utilization exceeds 70% for 5 minutes
- WHEN auto-scale evaluates
- THEN an additional instance is provisioned (up to max configured)
```

### What to Put in Spec Kit plan.md

```markdown
## Cloud Infrastructure

**Compute**: Azure App Service (Linux containers, B2 tier for MVP)
**Container Registry**: Azure Container Registry (Basic tier)
**Networking**: VNET integration, private endpoints for databases
**Secrets**: Azure Key Vault
**Monitoring**: Azure Monitor + Application Insights
**CI/CD**: GitHub Actions (build per service, deploy on merge to main)

## Deployment Strategy

- Each service has its own App Service instance
- Blue-green deployment via deployment slots (staging → production swap)
- Database migrations run as a pre-deployment step
- Rollback: swap back to previous slot
```

---

## 10. Heavy API Patterns

### OpenAPI Contracts First

For heavy API projects, write the OpenAPI spec BEFORE the code:

```
contracts/
├── order-api.yaml          # OpenAPI 3.x for Java order service
├── customer-api.yaml       # OpenAPI 3.x for .NET customer service
├── analytics-api.yaml      # OpenAPI 3.x for Python analytics service
└── shared-schemas.yaml     # Reusable: ErrorResponse, Pagination, etc.
```

### shared-schemas.yaml

```yaml
components:
  schemas:
    ErrorResponse:
      type: object
      required: [error]
      properties:
        error:
          type: object
          required: [code, message]
          properties:
            code:
              type: string
              example: "VALIDATION_ERROR"
            message:
              type: string
              example: "Invalid input"
            details:
              type: array
              items:
                type: object
                properties:
                  field:
                    type: string
                  message:
                    type: string

    PaginatedResponse:
      type: object
      properties:
        data:
          type: array
          items: {}
        pagination:
          type: object
          properties:
            page:
              type: integer
            pageSize:
              type: integer
            totalItems:
              type: integer
            totalPages:
              type: integer
```

### Mapping Contracts to Specs

```
contracts/order-api.yaml     →  openspec/specs/orders/spec.md
  Each path + method         →    One Requirement
  Each response code         →    One Scenario
  Request body schema        →    Scenario GIVEN conditions
  Response body schema       →    Scenario THEN conditions
```

---

## 11. Heavy Database Patterns

### OpenSpec for Current Schema Behavior

```markdown
# Orders Data Specification

## Purpose
Data integrity rules and constraints for the orders domain.

## Requirements

### Requirement: Order Integrity
The system MUST NOT allow an order with zero items.

#### Scenario: Empty order attempt
- GIVEN a request to create an order with no items
- WHEN the order is persisted
- THEN a validation error is raised
- AND no order record is created

### Requirement: Inventory Consistency
The system MUST decrement inventory atomically when an order is placed.

#### Scenario: Concurrent orders for last item
- GIVEN product X has 1 unit in inventory
- WHEN two orders for product X are submitted simultaneously
- THEN exactly one order succeeds
- AND the other receives a 409 Conflict
- AND inventory never goes negative

### Requirement: Cascade Behavior
The system MUST cascade-delete order items when an order is deleted.

#### Scenario: Order deletion
- GIVEN an order with 3 items
- WHEN the order is deleted
- THEN all 3 order item records are also deleted
```

### Spec Kit data-model.md

```markdown
# Data Model: Order Management

## Entities

### Order
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK, auto-generated |
| tracking_id | VARCHAR(20) | UNIQUE, NOT NULL |
| customer_id | UUID | FK → Customer.id, NOT NULL |
| status | ENUM | 'pending','confirmed','shipped','delivered','cancelled' |
| total | DECIMAL(12,2) | NOT NULL, >= 0 |
| created_at | TIMESTAMP | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMP | NOT NULL, DEFAULT NOW() |

### OrderItem
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| order_id | UUID | FK → Order.id ON DELETE CASCADE |
| product_id | UUID | FK → Product.id |
| quantity | INT | NOT NULL, > 0 |
| unit_price | DECIMAL(12,2) | NOT NULL, >= 0 |

### Product
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| name | VARCHAR(255) | NOT NULL |
| price | DECIMAL(12,2) | NOT NULL, >= 0 |
| inventory_count | INT | NOT NULL, >= 0 |
| updated_at | TIMESTAMP | NOT NULL |

## Relationships

```text
Customer 1 ──── * Order
Order    1 ──── * OrderItem
Product  1 ──── * OrderItem
```

## Indexes

- orders: (customer_id, created_at DESC) — for customer order history
- orders: (status) — for order processing queries
- order_items: (order_id) — for order detail lookup
- products: (inventory_count) WHERE inventory_count > 0 — for available products

## Migration Strategy

- Java/PostgreSQL: Flyway (V1__create_tables.sql, V2__add_indexes.sql)
- .NET/SQL Server: EF Core migrations (dotnet ef migrations add)
- Python/MongoDB: Schema-less, enforce via Pydantic validation
```

---

## 12. Quick-Start Recipes

### Recipe A: "I have legacy code with no docs"

```bash
# 1. Install OpenSpec
npm install -g @fission-ai/openspec@latest
cd your-project && openspec init

# 2. For each service/domain, create a spec:
#    openspec/specs/{domain}/spec.md
#    Read the code → write Requirements + Scenarios
#    Start with HIGH risk domains
#    Spend 30 min (Tier 1) to 4 hours (Tier 3) per domain

# 3. Done. You now have a living spec of your system.
#    Update specs as you discover new behavior.
```

### Recipe B: "I'm building something new from scratch"

```bash
# 1. Install Spec Kit
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@latest
cd your-project && specify init . --ai copilot

# 2. Write constitution.md (project-wide standards)
# 3. /speckit.specify → spec.md (user stories + requirements)
# 4. /speckit.clarify → resolve ambiguities
# 5. /speckit.plan → plan.md (tech stack + architecture)
# 6. /speckit.tasks → tasks.md (phased implementation)
# 7. /speckit.implement → code

# 8. After launch, add OpenSpec to capture current-state specs
```

### Recipe C: "Legacy code + new features" (Your case)

```bash
# 1. Install both
npm install -g @fission-ai/openspec@latest
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@latest

# 2. Initialize both in the same repo
cd your-project
openspec init
specify init . --ai copilot

# 3. Phase 0: Inventory your services (Section 4)
#    → Assign Tier 1/2/3 to each

# 4. Phase 1: Legacy rescue (OpenSpec)
#    For each HIGH-risk domain:
#    → Read code → openspec/specs/{domain}/spec.md
#    → Requirements + Scenarios for current behavior

# 5. Phase 2: Greenfield features (Spec Kit)
#    → Write constitution.md
#    → /speckit.specify → /speckit.plan → /speckit.tasks → /speckit.implement

# 6. Phase 3: Keep both in sync
#    After implementing a Spec Kit feature:
#    → Update OpenSpec specs to reflect new current state
#    After discovering undocumented legacy behavior:
#    → Add to OpenSpec specs immediately

# ONGOING WORKFLOW:
#   "What does it do?"  → Check openspec/specs/
#   "What are we building?" → Check .specify/specs/
#   "What changed?"     → Check openspec/changes/
```

### The One-Page Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                     YOUR PROJECT                                │
│                                                                 │
│  ┌──────────────────────┐    ┌──────────────────────┐          │
│  │     OpenSpec          │    │     Spec Kit          │          │
│  │  (Current State)      │    │  (New Features)       │          │
│  │                       │    │                       │          │
│  │  openspec/specs/      │    │  .specify/            │          │
│  │  ├── orders/          │    │  ├── constitution.md  │          │
│  │  ├── customers/       │    │  └── specs/           │          │
│  │  ├── analytics/       │    │      └── 001-feature/ │          │
│  │  └── infrastructure/  │    │          ├── spec.md  │          │
│  │                       │    │          ├── plan.md  │          │
│  │  "What does it do     │    │          └── tasks.md │          │
│  │   right now?"         │    │                       │          │
│  │                       │    │  "What are we         │          │
│  │  Tier 1: 30 min       │    │   building next?"     │          │
│  │  Tier 2: 2-4 hrs      │    │                       │          │
│  │  Tier 3: 4+ hrs       │    │  Tier 2: 2-4 hrs      │          │
│  │                       │    │  Tier 3: 1-2 days     │          │
│  └──────────────────────┘    └──────────────────────┘          │
│                                                                 │
│  contracts/              ← Shared OpenAPI specs (both use these)│
│  services/               ← Actual code (Java, .NET, Python)    │
│  infrastructure/         ← Bicep, Docker, CI/CD                │
└─────────────────────────────────────────────────────────────────┘
```
