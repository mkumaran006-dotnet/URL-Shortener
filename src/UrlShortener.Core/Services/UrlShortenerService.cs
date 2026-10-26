using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Core.Services;

public class UrlShortenerService(
    IUrlRepository urlRepo,
    IClickEventRepository clickRepo,
    ICacheService cache) : IUrlShortenerService
{
    private const string CacheKeyPrefix = "url:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private static readonly char[] Base62Chars =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    public async Task<ShortUrl> CreateAsync(
        string originalUrl, string? customAlias = null, string? title = null,
        DateTime? expiresAt = null, string? createdBy = null, CancellationToken ct = default)
    {
        var code = customAlias?.Trim().ToLowerInvariant() ?? GenerateCode();

        if (await urlRepo.ExistsAsync(code, ct))
            throw new DuplicateAliasException(code);

        var shortUrl = new ShortUrl
        {
            ShortCode = code,
            OriginalUrl = originalUrl,
            CustomAlias = customAlias,
            Title = title,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await urlRepo.AddAsync(shortUrl, ct);
        await cache.SetAsync(CacheKeyPrefix + code, created, CacheTtl, ct);
        return created;
    }

    public async Task<ShortUrl> ResolveAsync(
        string shortCode, string? ipAddress = null,
        string? userAgent = null, string? referrer = null, CancellationToken ct = default)
    {
        var url = await GetFromCacheOrDbAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        if (!url.IsActive)
            throw new UrlNotFoundException(shortCode);

        if (url.IsExpired)
            throw new UrlExpiredException(shortCode, url.ExpiresAt!.Value);

        // Fire-and-forget click record — don't block the redirect
        _ = Task.Run(async () =>
        {
            try
            {
                await urlRepo.IncrementClickCountAsync(url.Id, ct);
                await clickRepo.AddAsync(new ClickEvent
                {
                    ShortUrlId = url.Id,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Referrer = referrer,
                    ClickedAt = DateTime.UtcNow
                }, ct);
            }
            catch { /* telemetry/log in production */ }
        }, ct);

        return url;
    }

    public async Task<ShortUrl?> GetAsync(string shortCode, CancellationToken ct = default)
        => await GetFromCacheOrDbAsync(shortCode, ct);

    public async Task<IReadOnlyList<ShortUrl>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => await urlRepo.GetAllAsync(page, pageSize, ct);

    public async Task DeleteAsync(string shortCode, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        await urlRepo.DeleteAsync(url.Id, ct);
        await cache.RemoveAsync(CacheKeyPrefix + shortCode, ct);
    }

    public async Task<ShortUrl> UpdateAsync(
        string shortCode, string? title = null,
        DateTime? expiresAt = null, bool? isActive = null, CancellationToken ct = default)
    {
        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct)
                  ?? throw new UrlNotFoundException(shortCode);

        if (title is not null) url.Title = title;
        if (expiresAt.HasValue) url.ExpiresAt = expiresAt;
        if (isActive.HasValue) url.IsActive = isActive.Value;

        var updated = await urlRepo.UpdateAsync(url, ct);
        await cache.SetAsync(CacheKeyPrefix + shortCode, updated, CacheTtl, ct);
        return updated;
    }

    private async Task<ShortUrl?> GetFromCacheOrDbAsync(string shortCode, CancellationToken ct)
    {
        var cached = await cache.GetAsync<ShortUrl>(CacheKeyPrefix + shortCode, ct);
        if (cached is not null) return cached;

        var url = await urlRepo.GetByShortCodeAsync(shortCode, ct);
        if (url is not null)
            await cache.SetAsync(CacheKeyPrefix + shortCode, url, CacheTtl, ct);
        return url;
    }

    private static string GenerateCode()
    {
        // Base62 encoding of random bytes → 6-char code with ~56 billion combinations
        var bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        var value = Math.Abs(BitConverter.ToInt64(bytes, 0));
        return EncodeBase62(value, 6);
    }

    private static string EncodeBase62(long value, int minLength)
    {
        var chars = new char[minLength];
        for (var i = minLength - 1; i >= 0; i--)
        {
            chars[i] = Base62Chars[value % 62];
            value /= 62;
        }
        return new string(chars);
    }
}
