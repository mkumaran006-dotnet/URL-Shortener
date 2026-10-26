using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;
using UrlShortener.Infrastructure.Data;

namespace UrlShortener.Infrastructure.Repositories;

public class UrlRepository(AppDbContext db) : IUrlRepository
{
    public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
        => db.ShortUrls.FirstOrDefaultAsync(x => x.ShortCode == shortCode, ct);

    public Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default)
        => db.ShortUrls.FindAsync([id], ct).AsTask();

    public async Task<IReadOnlyList<ShortUrl>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await db.ShortUrls
                   .OrderByDescending(x => x.CreatedAt)
                   .Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync(ct);

    public async Task<ShortUrl> AddAsync(ShortUrl url, CancellationToken ct = default)
    {
        db.ShortUrls.Add(url);
        await db.SaveChangesAsync(ct);
        return url;
    }

    public async Task<ShortUrl> UpdateAsync(ShortUrl url, CancellationToken ct = default)
    {
        db.ShortUrls.Update(url);
        await db.SaveChangesAsync(ct);
        return url;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await db.ShortUrls.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }

    public Task<bool> ExistsAsync(string shortCode, CancellationToken ct = default)
        => db.ShortUrls.AnyAsync(x => x.ShortCode == shortCode, ct);

    public async Task IncrementClickCountAsync(long id, CancellationToken ct = default)
    {
        await db.ShortUrls
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClickCount, x => x.ClickCount + 1), ct);
    }

    public Task<long> GetTotalCountAsync(CancellationToken ct = default)
        => db.ShortUrls.LongCountAsync(ct);
}
