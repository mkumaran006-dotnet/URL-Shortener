using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

public interface IUrlShortenerService
{
    Task<ShortUrl> CreateAsync(string originalUrl, string? customAlias = null, string? title = null, DateTime? expiresAt = null, string? createdBy = null, CancellationToken ct = default);
    Task<ShortUrl> ResolveAsync(string shortCode, string? ipAddress = null, string? userAgent = null, string? referrer = null, CancellationToken ct = default);
    Task<ShortUrl?> GetAsync(string shortCode, CancellationToken ct = default);
    Task<IReadOnlyList<ShortUrl>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task DeleteAsync(string shortCode, CancellationToken ct = default);
    Task<ShortUrl> UpdateAsync(string shortCode, string? title = null, DateTime? expiresAt = null, bool? isActive = null, CancellationToken ct = default);
}
