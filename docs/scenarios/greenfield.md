# Scenario 1: Greenfield — Build From Scratch

## Summary

Build the URL shortener service from a blank slate. All six SDLC stages execute end-to-end, with Architecture and ReleaseReadiness requiring human approval.

## Requirement Input

```
Build a URL shortener service with:
- Core REST API (create, resolve, list, update, delete)
- Click analytics (total count, daily breakdown, top referrers, country breakdown)
- Reliability features (rate limiting, caching, expiry)
- Clean architecture with dependency injection
- SQLite database (dev), PostgreSQL-ready (production)
- Swagger/OpenAPI documentation
- 80%+ test coverage
```

## Orchestration Walkthrough

### Stage 1: Requirements
- **Input:** Raw requirement text
- **Processing:** Normalize, identify ambiguities, derive acceptance criteria
- **Ambiguities resolved:**
  - Storage backend → defaults to SQLite for dev
  - Authentication → deferred to v2 (API keys)
- **Outputs:** `normalized_spec`, `ambiguities`, `constraints`, `acceptance_criteria`
- **Gate:** Exit requires `normalized_spec` present

### Stage 2: Architecture ← Human Approval Required
- **Input:** `normalized_spec` from Stage 1
- **Processing:** Generate ADRs, component diagram, API schema
- **Decision Lineage:**
  - ADR-001: Base62 short code generation
  - ADR-002: In-memory cache (no Redis dependency)
  - ADR-003: Async click recording
  - ADR-004: Clean architecture layering
  - ADR-005: EF Core + SQLite
- **Gate:** Exit requires `adrs`, `components`, `api_schema`
- **Approval:** Human reviews design before implementation begins

### Stages 3a & 3b: Implementation + Documentation (parallel)
- **Implementation** produces the 14 source files catalogued in the manifest
- **Documentation** runs in parallel after Architecture (no dependency on Implementation)
- Both gates enforce artifact presence

### Stage 4: Testing
- **Depends on:** Implementation only
- **Produces:** 18 unit tests + 6 integration tests (24 total for greenfield)
- **Gate:** Exit requires `all_tests_pass = true`

### Stage 5: ReleaseReadiness ← Human Approval Required
- **Depends on:** Testing AND Documentation
- **Evaluates** 9 release checks (security, compliance, documentation, test coverage)
- **Gate:** Exit requires `release_ready = true`
- **Approval:** Human sign-off before release

## Execution Graph

```
Requirements (serial)
   │
Architecture (serial, approval)
   │
   ├── Implementation ──────── (parallel)
   └── Documentation ──────── (parallel)
            │ (both complete)
         Testing (serial)
            │
      ReleaseReadiness (serial, approval)
```

## Outcome

- 6 stages completed, 2 human approvals obtained
- 5 ADRs recorded in decision lineage
- 14 code artifacts catalogued
- 9/9 release checks pass
- Workflow status: **Completed**

## Risks and Trade-offs

| Risk | Mitigation |
|------|-----------|
| In-memory cache lost on restart | Document Redis upgrade path in ADR-002 |
| SQLite not suitable for multi-instance | Connection string only change to PostgreSQL |
| No authentication in MVP | ADR explicitly defers to v2 |
| Async click lag under failure | Accepted trade-off for latency; counter is eventually consistent |
