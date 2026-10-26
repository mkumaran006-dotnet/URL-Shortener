using Moq;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;

namespace UrlShortener.Core.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IUrlRepository> _urlRepo = new();
    private readonly Mock<IClickEventRepository> _clickRepo = new();
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _sut = new AnalyticsService(_urlRepo.Object, _clickRepo.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsSummary_WhenExists()
    {
        var url = new ShortUrl
        {
            ShortCode = "abc", OriginalUrl = "https://test.com",
            ClickCount = 42, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _urlRepo.Setup(r => r.GetByShortCodeAsync("abc", default)).ReturnsAsync(url);

        var result = await _sut.GetSummaryAsync("abc");

        Assert.Equal("abc", result.ShortCode);
        Assert.Equal(42, result.TotalClicks);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetSummary_Throws_WhenNotFound()
    {
        _urlRepo.Setup(r => r.GetByShortCodeAsync("missing", default)).ReturnsAsync((ShortUrl?)null);
        await Assert.ThrowsAsync<UrlNotFoundException>(() => _sut.GetSummaryAsync("missing"));
    }

    [Fact]
    public async Task GetDailyClicks_ReturnsMappedStats()
    {
        var url = new ShortUrl { Id = 1, ShortCode = "abc" };
        _urlRepo.Setup(r => r.GetByShortCodeAsync("abc", default)).ReturnsAsync(url);

        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var raw = new List<(DateTime, long)> { (DateTime.UtcNow.Date, 10L), (DateTime.UtcNow.AddDays(-1).Date, 5L) };
        _clickRepo.Setup(r => r.GetDailyClicksAsync(1, from, to, default)).ReturnsAsync(raw);

        var result = await _sut.GetDailyClicksAsync("abc", from, to);

        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].Count);
    }

    [Fact]
    public async Task GetTopReferrers_ReturnsMappedReferrers()
    {
        var url = new ShortUrl { Id = 1, ShortCode = "abc" };
        _urlRepo.Setup(r => r.GetByShortCodeAsync("abc", default)).ReturnsAsync(url);
        _clickRepo.Setup(r => r.GetTopReferrersAsync(1, 5, default))
                  .ReturnsAsync([("https://google.com", 50L), ("direct", 10L)]);

        var result = await _sut.GetTopReferrersAsync("abc", 5);

        Assert.Equal(2, result.Count);
        Assert.Equal("https://google.com", result[0].Referrer);
    }
}
