using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;
using UrlShortener.Infrastructure.Data;

namespace UrlShortener.Infrastructure.Repositories;

public class ClickEventRepository(AppDbContext db) : IClickEventRepository
{
    public async Task AddAsync(ClickEvent clickEvent, CancellationToken ct = default)
    {
        db.ClickEvents.Add(clickEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ClickEvent>> GetByShortUrlIdAsync(
        long shortUrlId, int page, int pageSize, CancellationToken ct = default)
        => await db.ClickEvents
                   .Where(x => x.ShortUrlId == shortUrlId)
                   .OrderByDescending(x => x.ClickedAt)
                   .Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync(ct);

    public Task<long> GetClickCountAsync(long shortUrlId, CancellationToken ct = default)
        => db.ClickEvents.LongCountAsync(x => x.ShortUrlId == shortUrlId, ct);

    public async Task<IReadOnlyList<(DateTime Date, long Count)>> GetDailyClicksAsync(
        long shortUrlId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var results = await db.ClickEvents
            .Where(x => x.ShortUrlId == shortUrlId && x.ClickedAt >= from && x.ClickedAt <= to)
            .GroupBy(x => x.ClickedAt.Date)
            .Select(g => new { Date = g.Key, Count = (long)g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return results.Select(r => (r.Date, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(string Referrer, long Count)>> GetTopReferrersAsync(
        long shortUrlId, int top = 10, CancellationToken ct = default)
    {
        var results = await db.ClickEvents
            .Where(x => x.ShortUrlId == shortUrlId && x.Referrer != null)
            .GroupBy(x => x.Referrer!)
            .Select(g => new { Referrer = g.Key, Count = (long)g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(ct);

        return results.Select(r => (r.Referrer, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(string Country, long Count)>> GetClicksByCountryAsync(
        long shortUrlId, CancellationToken ct = default)
    {
        var results = await db.ClickEvents
            .Where(x => x.ShortUrlId == shortUrlId && x.Country != null)
            .GroupBy(x => x.Country!)
            .Select(g => new { Country = g.Key, Count = (long)g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return results.Select(r => (r.Country, r.Count)).ToList();
    }
}
