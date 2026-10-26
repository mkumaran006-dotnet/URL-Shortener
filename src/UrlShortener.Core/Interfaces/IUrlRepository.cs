using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

public interface IUrlRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default);
    Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ShortUrl>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ShortUrl> AddAsync(ShortUrl url, CancellationToken ct = default);
    Task<ShortUrl> UpdateAsync(ShortUrl url, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string shortCode, CancellationToken ct = default);
    Task IncrementClickCountAsync(long id, CancellationToken ct = default);
    Task<long> GetTotalCountAsync(CancellationToken ct = default);
}
