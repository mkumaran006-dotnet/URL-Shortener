using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

public interface IClickEventRepository
{
    Task AddAsync(ClickEvent clickEvent, CancellationToken ct = default);
    Task<IReadOnlyList<ClickEvent>> GetByShortUrlIdAsync(long shortUrlId, int page, int pageSize, CancellationToken ct = default);
    Task<long> GetClickCountAsync(long shortUrlId, CancellationToken ct = default);
    Task<IReadOnlyList<(DateTime Date, long Count)>> GetDailyClicksAsync(long shortUrlId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<(string Referrer, long Count)>> GetTopReferrersAsync(long shortUrlId, int top = 10, CancellationToken ct = default);
    Task<IReadOnlyList<(string Country, long Count)>> GetClicksByCountryAsync(long shortUrlId, CancellationToken ct = default);
}
