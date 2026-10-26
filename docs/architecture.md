# Architecture Overview

## System Design

### Layered Architecture

```
┌─────────────────────────────────────────────────────┐
│  UrlShortener.Api  (Presentation Layer)              │
│  Controllers · Middleware · Models · Program.cs      │
└────────────────────────┬────────────────────────────┘
                         │ references
┌────────────────────────▼────────────────────────────┐
│  UrlShortener.Core  (Domain Layer)                   │
│  Entities · Interfaces · Services · Exceptions       │
└────────────────────────▲────────────────────────────┘
                         │ implements
┌────────────────────────┴────────────────────────────┐
│  UrlShortener.Infrastructure  (Data Layer)           │
│  EF Core + SQLite · Repositories · In-Memory Cache  │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  UrlShortener.Orchestration  (Orchestration Engine)  │
│  Engine · Stages · Gates · Governance · Reliability  │
└─────────────────────────────────────────────────────┘
```

The Orchestration layer is independent of the URL shortener domain — it is a general-purpose SDLC automation engine.

---

## Orchestration Engine Design

The orchestration engine is the **critical differentiator** of this system. It implements an agentic execution model for automating the full SDLC lifecycle.

### Core Concepts

**Dependency Graph (`DependencyGraph`)**
- Directed Acyclic Graph of stage definitions
- Topological sort determines execution order
- Detects cycles and missing dependencies at validation time
- Exposes parallel batches: stages that share the same dependency frontier run concurrently

**Stage (`IStage`)**
- Discrete unit of SDLC work (Requirements → Architecture → Implementation → Testing → Documentation → ReleaseReadiness)
- Reads upstream outputs from `WorkflowContext`
- Writes outputs and artifacts to `StageResult`

**Entry Gates (`EntryGateDelegate`)**
- Validate pre-conditions before a stage begins
- Composable: `EntryGate.All(gate1, gate2)`
- Examples: `RequireStagesCompleted`, `RequireOutput`

**Exit Gates (`ExitGateDelegate`)**
- Validate post-conditions after a stage completes
- Block the stage from being marked Completed if outputs are missing
- Examples: `RequireOutputs`, `RequireArtifacts`

**Human Approval Checkpoints (`IApprovalService`)**
- Architecture and ReleaseReadiness stages require human approval
- `AutoApprovalService` simulates this in demo/test mode
- Production: swap for ServiceNow/Jira/Slack approval workflow

**Policy Guardrails (`PolicyGuardrailService`)**
- Evaluated before each stage executes
- `SEC-001`: Detect potential plaintext secrets in code artifacts
- `CC-001`: Enforce change control (Architecture must precede Implementation)
- Violations at `Critical` severity halt the workflow

**Retry Policy (`RetryPolicy`)**
- Bounded retries with exponential back-off
- Per-stage configuration: `Default` (3 attempts), `Aggressive` (5), `NoRetry` (1)
- Capped at `MaxDelay` to prevent unbounded waits

**Rollback Manager (`RollbackManager`)**
- Stages register rollback handlers on engine startup
- On failure of a stage with `SupportsRollback = true`, completed upstream stages are rolled back in reverse chronological order
- Audit-logged with `Critical` severity

**Audit Logger (`AuditLogger`)**
- Every gate evaluation, stage start/end, retry, approval, policy violation, and rollback is logged
- Entries travel with the `WorkflowContext` for full portability
- Severity levels: Info, Warning, Error, Critical

**Metrics (`WorkflowMetrics`)**
- Tracks: `SuccessRate`, `TotalRetries`, `TotalRollbacks`, `TotalDuration`, `MttrSeconds`
- Per-stage `StageDuration` records with recovery flag

**Decision Lineage (`DecisionRecord`)**
- Every non-trivial decision is recorded with rationale and context
- Preserved in `WorkflowContext.DecisionLineage` across all stages
- Surfaced in the `/api/orchestration/{id}` response

### SDLC Stage Dependency Graph

```
Requirements
     │
     ▼
Architecture ←── [Human Approval]
     │
     ├──────────────────────┐
     ▼                      ▼
Implementation         Documentation
     │
     ▼
  Testing ─────────────────────┐
                               ▼
                      ReleaseReadiness ←── [Human Approval]
```

- **Testing** and **Documentation** run in parallel after their respective dependencies complete
- Both parallel stages must complete before **ReleaseReadiness** can begin
- Two human approvals: Architecture (high-impact design decision) and ReleaseReadiness (release gate)

---

## Architecture Decision Records

### ADR-001: Base62 Short Code Generation
**Decision:** Generate 6-character Base62-encoded codes from random bytes.
**Rationale:** URL-safe, dense encoding (~56 billion combinations in 6 characters), avoids ambiguous characters (0/O, 1/l). No coordination required between instances.
**Status:** Accepted

### ADR-002: In-Process In-Memory Cache
**Decision:** Use `IMemoryCache` for caching hot URL lookups.
**Rationale:** Eliminates Redis dependency for MVP; reduces DB round-trips for frequently accessed URLs. Trade-off: cache is node-local, lost on restart.
**Status:** Accepted (with documented production upgrade path to Redis)

### ADR-003: Async Fire-and-Forget Click Recording
**Decision:** Record click events asynchronously without blocking the redirect response.
**Rationale:** Redirect latency must stay under 50 ms P99. Synchronous click recording would add 5–15 ms. Trade-off: under failure, click count may lag by one record.
**Status:** Accepted

### ADR-004: Clean Architecture Layering
**Decision:** Separate Core (domain), Infrastructure (data), API (presentation), and Orchestration (engine) into distinct projects.
**Rationale:** Enforces the Dependency Inversion Principle. API and Infrastructure both depend on Core abstractions. Swapping SQLite for PostgreSQL requires only a connection string change.
**Status:** Accepted

### ADR-005: EF Core + SQLite (dev), PostgreSQL-ready (production)
**Decision:** Use EF Core with SQLite for zero-config local development.
**Rationale:** Developers can run the full stack with `dotnet run` — no Docker or external database required. EF Core abstracts the provider; production switches by changing the connection string.
**Status:** Accepted

### ADR-006: Delegate-Based Gate Composition
**Decision:** Define entry/exit gates as delegate types (`EntryGateDelegate`, `ExitGateDelegate`) rather than interfaces.
**Rationale:** Enables composable, functional-style gate definitions without ceremony. `EntryGate.All(gate1, gate2)` composes without inheritance.
**Status:** Accepted

---

## Reliability Features

| Feature | Implementation |
|---------|---------------|
| Rate Limiting | Sliding-window per-IP middleware |
| Caching | Cache-aside pattern with configurable TTL |
| URL Expiry | ISO 8601 `ExpiresAt` field; 410 Gone on expiry |
| Retry | Exponential back-off with jitter in orchestration engine |
| Rollback | Stage-level rollback on workflow failure |
| Health Check | `/health` endpoint via EF Core DbContextCheck |
| Graceful Shutdown | `SafeStop` cancels in-flight workflow via `CancellationToken` |
| Audit Trail | Structured audit log on every state transition |

---

## Security Controls

- Input validation on all request models (`[Required]`, `[Url]`, `[MaxLength]`, `[RegularExpression]`)
- Rate limiting per IP (configurable, 100 req/min default)
- RFC 7807 `application/problem+json` error responses (no internal detail leak)
- `ExceptionHandlingMiddleware` prevents stack trace exposure
- HTTPS redirect enforced in production
- Policy guardrail `SEC-001` checks for plaintext secrets in code artifacts

---

## Testing Approach

| Layer | Strategy |
|-------|----------|
| Core services | Unit tests with Moq mocks; full boundary and error-path coverage |
| API controllers | Unit tests with mocked services; validates HTTP status codes and response shapes |
| Orchestration engine | Integration tests against real engine (no mocks); validates full workflow execution, audit log, metrics, and all three scenario types |
| Dependency graph | Unit tests for topological sort correctness, parallel batch extraction, and cycle detection |

**Coverage target:** 80% line coverage across all projects.
