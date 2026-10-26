namespace UrlShortener.Api.Models.Responses;

public record AnalyticsSummaryResponse(
    string ShortCode,
    string OriginalUrl,
    long TotalClicks,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsActive);

public record DailyClickResponse(DateTime Date, long Count);
public record ReferrerResponse(string Referrer, long Count);
public record CountryResponse(string Country, long Count);
