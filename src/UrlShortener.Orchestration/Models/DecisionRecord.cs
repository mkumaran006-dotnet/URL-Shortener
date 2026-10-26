namespace UrlShortener.Orchestration.Models;

public record DecisionRecord(
    DateTime Timestamp,
    string Stage,
    string Decision,
    string Rationale,
    string MadeBy,
    IReadOnlyDictionary<string, object?> Context);
