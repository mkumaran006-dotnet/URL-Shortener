# Scenario 3: Ambiguous — "Make It Faster and More Reliable"

## Summary

The input requirement is deliberately vague: `"Make the URL shortener faster and more reliable."` This scenario demonstrates the orchestration engine's **requirement clarification capability** — the Requirements stage resolves ambiguity, records its interpretations as decision lineage, and produces a concrete engineering specification before any implementation work begins.

## Requirement Input

```
Make the URL shortener faster and more reliable.
```

## Ambiguity Resolution

The Requirements stage applies heuristics to normalize the ambiguous request:

| Ambiguous Term | Engineering Interpretation | Rationale |
|----------------|--------------------------|-----------|
| "Faster" | Cache hot URLs in-memory; add composite DB index on ShortCode; enable response compression | Redirect is the hot path — caching eliminates DB round-trip for repeated requests |
| "More reliable" | Add `/health` endpoint; transient retry on DB errors; graceful shutdown on SIGTERM | Standard reliability primitives for a web service |

These interpretations are recorded as `DecisionRecord` entries with `madeBy = "requirements-agent"` and `rationale` explaining the heuristic applied.

## How the Orchestration Engine Handles Ambiguity

1. **Requirements stage** detects that the input lacks quantitative targets or specific features
2. It applies `ScenarioType.Ambiguous` logic to generate 2+ clarification notes
3. Clarifications are stored in `ambiguities` output key and recorded in `DecisionLineage`
4. Downstream stages (Architecture, Implementation) consume `normalized_spec` which embeds the resolved interpretation
5. A human reviewer sees the clarification notes at the Architecture approval checkpoint and can reject/modify if the interpretation is wrong

## Orchestration Differences vs Greenfield

- **Requirements:** 2 ambiguity notes (vs 2 for Greenfield, but different content)
- **Architecture:** Additional ADR for caching specifically framed as a performance requirement
- **Implementation:** Adds health check controller files to the artifact manifest
- **Testing:** 3 additional unit tests + 2 integration tests (smoke test for latency; chaos injection for retry)
- **Documentation:** Scenario-specific notes explain ambiguity resolution

## Governance Value

This scenario demonstrates a key principle: **agents should surface interpretations for human review rather than silently choosing**. The audit log contains a complete record of:
- The original ambiguous input
- The clarifications applied
- The rationale for each clarification
- The human approval at Architecture (where the reviewer sees the resolved spec)

If the human disagrees with an interpretation (e.g., "faster" should mean adding CDN, not in-memory cache), they can reject the Architecture stage and provide updated input for re-planning.

## Risks and Trade-offs

| Risk | Mitigation |
|------|-----------|
| Wrong interpretation of "faster" | Human approval at Architecture checkpoint surfaces the interpretation |
| Performance improvements insufficient | Acceptance criteria require measurable targets (< 50 ms redirect P99) |
| "Reliable" without SLA definition | Default to health check + retry; SLA definition deferred to human review |
| Over-engineering for ambiguous scope | Orchestration engine produces minimum viable interpretation; human can expand |
