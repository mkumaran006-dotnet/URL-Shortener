using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Core.Services;

public class AnalyticsService(
    IUrlRepository urlRepo,
    IClickEventRepository clickRepo) : IAnalyticsService
{
    public async Task<AnalyticsSummary> GetSummaryAsync(string shortCode, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        return new AnalyticsSummary(
            url.ShortCode,
            url.OriginalUrl,
            url.ClickCount,
            url.CreatedAt,
            url.ExpiresAt,
            url.IsActive);
    }

    public async Task<IReadOnlyList<DailyClickStat>> GetDailyClicksAsync(
        string shortCode, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        var raw = await clickRepo.GetDailyClicksAsync(url.Id, from, to, ct);
        return raw.Select(r => new DailyClickStat(r.Date, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<ReferrerStat>> GetTopReferrersAsync(
        string shortCode, int top = 10, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        var raw = await clickRepo.GetTopReferrersAsync(url.Id, top, ct);
        return raw.Select(r => new ReferrerStat(r.Referrer, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<CountryStat>> GetCountryBreakdownAsync(
        string shortCode, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        var raw = await clickRepo.GetClicksByCountryAsync(url.Id, ct);
        return raw.Select(r => new CountryStat(r.Country, r.Count)).ToList();
    }
}
