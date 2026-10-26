# Scenario 2: Brownfield — Enhance Existing Service

## Summary

Add analytics, rate limiting, and URL expiry to an existing URL shortener that has only basic CRUD. The orchestration engine identifies impacted modules, enforces backward compatibility constraints, and produces an enhancement plan that preserves the existing API contract.

## Requirement Input

```
Enhance the existing URL shortener:
1. Add click analytics API (GET /api/urls/{code}/analytics)
2. Add per-IP rate limiting (100 req/min)
3. Add URL expiry support (ExpiresAt field)
CONSTRAINT: Existing short codes must continue to resolve unchanged.
CONSTRAINT: No breaking changes to existing API contract.
```

## Codebase Impact Analysis

| Module | Impact |
|--------|--------|
| `ShortUrl` entity | ADD: ClickCount, ExpiresAt, IsActive fields |
| `UrlRepository` | ADD: IncrementClickCountAsync; UPDATE: queries to respect IsActive/ExpiresAt |
| `Program.cs` | ADD: rate limiting middleware, health check |
| NEW: `ClickEvent` entity | Full new entity |
| NEW: `ClickEventRepository` | Full new repository |
| NEW: `AnalyticsController` | Full new controller |
| NEW: `AnalyticsService` | Full new service |

## Orchestration Differences vs Greenfield

### Requirements Stage
- **Additional constraint captured:** `"Backward compatibility: existing short codes must continue to resolve."`
- **Acceptance criteria added:** `"Existing short codes resolve without modification."`

### Architecture Stage
- Component diagram scoped to new/modified components only
- ADRs include: `"No ALTER TABLE — add columns with defaults"` for schema migration safety
- Change control policy (CC-001) enforced: Architecture must precede Implementation

### Testing Stage
- **4 additional regression tests** for existing short code resolution
- **2 additional integration tests** for backward compatibility

### ReleaseReadiness
- Same 9 checks; backward compatibility treated as an additional acceptance criterion

## Key Orchestration Behaviors Demonstrated

1. **Brownfield context propagation:** `impacted_modules` key in workflow input is carried through all stages
2. **Policy guardrail CC-001:** Enforces Architecture approval before Implementation can start
3. **Decision lineage:** Documents the backward-compatibility constraint as a first-class decision
4. **Regression test expansion:** Testing stage increases test count based on scenario type

## Risks and Trade-offs

| Risk | Mitigation |
|------|-----------|
| Schema migration breaks existing data | Use EF Core migrations with defaults for new nullable columns |
| Breaking change to API shape | Exit gate validates API contract preservation |
| Cache invalidation on entity changes | Cache TTL + explicit invalidation on update/delete |
| Rate limiter state lost on restart | Acceptable for MVP; Redis-backed for production |
