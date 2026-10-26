namespace UrlShortener.Core.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsSummary> GetSummaryAsync(string shortCode, CancellationToken ct = default);
    Task<IReadOnlyList<DailyClickStat>> GetDailyClicksAsync(string shortCode, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<ReferrerStat>> GetTopReferrersAsync(string shortCode, int top = 10, CancellationToken ct = default);
    Task<IReadOnlyList<CountryStat>> GetCountryBreakdownAsync(string shortCode, CancellationToken ct = default);
}

public record AnalyticsSummary(
    string ShortCode,
    string OriginalUrl,
    long TotalClicks,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsActive);

public record DailyClickStat(DateTime Date, long Count);
public record ReferrerStat(string Referrer, long Count);
public record CountryStat(string Country, long Count);
