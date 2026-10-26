using Moq;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;

namespace UrlShortener.Core.Tests.Services;

public class UrlShortenerServiceTests
{
    private readonly Mock<IUrlRepository> _urlRepo = new();
    private readonly Mock<IClickEventRepository> _clickRepo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly UrlShortenerService _sut;

    public UrlShortenerServiceTests()
    {
        _sut = new UrlShortenerService(_urlRepo.Object, _clickRepo.Object, _cache.Object);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_GeneratesShortCode_WhenNoAlias()
    {
        _urlRepo.Setup(r => r.ExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _urlRepo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), default))
                .ReturnsAsync((ShortUrl u, CancellationToken _) => u);

        var result = await _sut.CreateAsync("https://example.com");

        Assert.NotEmpty(result.ShortCode);
        Assert.Equal("https://example.com", result.OriginalUrl);
    }

    [Fact]
    public async Task Create_UsesCustomAlias_WhenProvided()
    {
        _urlRepo.Setup(r => r.ExistsAsync("myalias", default)).ReturnsAsync(false);
        _urlRepo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), default))
                .ReturnsAsync((ShortUrl u, CancellationToken _) => u);

        var result = await _sut.CreateAsync("https://example.com", "myalias");

        Assert.Equal("myalias", result.ShortCode);
    }

    [Fact]
    public async Task Create_Throws_WhenAliasAlreadyExists()
    {
        _urlRepo.Setup(r => r.ExistsAsync("taken", default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateAliasException>(() =>
            _sut.CreateAsync("https://example.com", "taken"));
    }

    // ── Resolve ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_ReturnsUrl_WhenValid()
    {
        var url = new ShortUrl { Id = 1, ShortCode = "abc123", OriginalUrl = "https://example.com", IsActive = true };
        _cache.Setup(c => c.GetAsync<ShortUrl>("url:abc123", default)).ReturnsAsync((ShortUrl?)null);
        _urlRepo.Setup(r => r.GetByShortCodeAsync("abc123", default)).ReturnsAsync(url);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ShortUrl>(), It.IsAny<TimeSpan?>(), default))
              .Returns(Task.CompletedTask);

        var result = await _sut.ResolveAsync("abc123");

        Assert.Equal("https://example.com", result.OriginalUrl);
    }

    [Fact]
    public async Task Resolve_Throws_WhenNotFound()
    {
        _cache.Setup(c => c.GetAsync<ShortUrl>(It.IsAny<string>(), default)).ReturnsAsync((ShortUrl?)null);
        _urlRepo.Setup(r => r.GetByShortCodeAsync("missing", default)).ReturnsAsync((ShortUrl?)null);

        await Assert.ThrowsAsync<UrlNotFoundException>(() => _sut.ResolveAsync("missing"));
    }

    [Fact]
    public async Task Resolve_Throws_WhenExpired()
    {
        var url = new ShortUrl
        {
            Id = 1, ShortCode = "old", OriginalUrl = "https://example.com",
            IsActive = true, ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _cache.Setup(c => c.GetAsync<ShortUrl>("url:old", default)).ReturnsAsync((ShortUrl?)null);
        _urlRepo.Setup(r => r.GetByShortCodeAsync("old", default)).ReturnsAsync(url);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ShortUrl>(), It.IsAny<TimeSpan?>(), default))
              .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<UrlExpiredException>(() => _sut.ResolveAsync("old"));
    }

    [Fact]
    public async Task Resolve_Throws_WhenInactive()
    {
        var url = new ShortUrl { Id = 1, ShortCode = "off", OriginalUrl = "https://example.com", IsActive = false };
        _cache.Setup(c => c.GetAsync<ShortUrl>("url:off", default)).ReturnsAsync((ShortUrl?)null);
        _urlRepo.Setup(r => r.GetByShortCodeAsync("off", default)).ReturnsAsync(url);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ShortUrl>(), It.IsAny<TimeSpan?>(), default))
              .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<UrlNotFoundException>(() => _sut.ResolveAsync("off"));
    }

    // ── Get from cache ────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsCachedValue_WithoutHittingDb()
    {
        var cached = new ShortUrl { ShortCode = "xyz", OriginalUrl = "https://cached.com" };
        _cache.Setup(c => c.GetAsync<ShortUrl>("url:xyz", default)).ReturnsAsync(cached);

        var result = await _sut.GetAsync("xyz");

        Assert.Equal("https://cached.com", result?.OriginalUrl);
        _urlRepo.Verify(r => r.GetByShortCodeAsync(It.IsAny<string>(), default), Times.Never);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesFromCacheAndDb()
    {
        var url = new ShortUrl { Id = 5, ShortCode = "del1" };
        _urlRepo.Setup(r => r.GetByShortCodeAsync("del1", default)).ReturnsAsync(url);
        _urlRepo.Setup(r => r.DeleteAsync(5, default)).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync("url:del1", default)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync("del1");

        _urlRepo.Verify(r => r.DeleteAsync(5, default), Times.Once);
        _cache.Verify(c => c.RemoveAsync("url:del1", default), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenNotFound()
    {
        _urlRepo.Setup(r => r.GetByShortCodeAsync("gone", default)).ReturnsAsync((ShortUrl?)null);
        await Assert.ThrowsAsync<UrlNotFoundException>(() => _sut.DeleteAsync("gone"));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesTitle_AndRefreshesCache()
    {
        var url = new ShortUrl { Id = 1, ShortCode = "upd", Title = "Old" };
        _urlRepo.Setup(r => r.GetByShortCodeAsync("upd", default)).ReturnsAsync(url);
        _urlRepo.Setup(r => r.UpdateAsync(It.IsAny<ShortUrl>(), default))
                .ReturnsAsync((ShortUrl u, CancellationToken _) => u);

        var result = await _sut.UpdateAsync("upd", title: "New");

        Assert.Equal("New", result.Title);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ShortUrl>(), It.IsAny<TimeSpan?>(), default), Times.Once);
    }
}
