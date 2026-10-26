using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.Models.Requests;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Api.Tests.Controllers;

public class UrlsControllerTests
{
    private readonly Mock<IUrlShortenerService> _svc = new();
    private readonly Mock<IHttpContextAccessor> _ctx = new();
    private readonly UrlsController _sut;

    public UrlsControllerTests()
    {
        var httpCtx = new Mock<HttpContext>();
        var req = new Mock<HttpRequest>();
        req.Setup(r => r.Scheme).Returns("https");
        req.Setup(r => r.Host).Returns(new HostString("short.example.com"));
        httpCtx.Setup(c => c.Request).Returns(req.Object);
        _ctx.Setup(c => c.HttpContext).Returns(httpCtx.Object);

        _sut = new UrlsController(_svc.Object, _ctx.Object);
    }

    [Fact]
    public async Task Create_Returns201_WithShortUrl()
    {
        var url = new ShortUrl { ShortCode = "abc123", OriginalUrl = "https://example.com" };
        _svc.Setup(s => s.CreateAsync("https://example.com", null, null, null, null, default))
            .ReturnsAsync(url);

        var result = await _sut.Create(new CreateUrlRequest { OriginalUrl = "https://example.com" }, default);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task GetByCode_Returns200_WhenFound()
    {
        var url = new ShortUrl { ShortCode = "abc", OriginalUrl = "https://example.com" };
        _svc.Setup(s => s.GetAsync("abc", default)).ReturnsAsync(url);

        var result = await _sut.GetByCode("abc", default);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByCode_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.GetAsync("missing", default)).ReturnsAsync((ShortUrl?)null);

        var result = await _sut.GetByCode("missing", default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Returns204_WhenSuccessful()
    {
        _svc.Setup(s => s.DeleteAsync("abc", default)).Returns(Task.CompletedTask);

        var result = await _sut.Delete("abc", default);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task List_ReturnsPaged_Results()
    {
        var urls = new List<ShortUrl>
        {
            new() { ShortCode = "a", OriginalUrl = "https://a.com" },
            new() { ShortCode = "b", OriginalUrl = "https://b.com" }
        };
        _svc.Setup(s => s.ListAsync(1, 20, default)).ReturnsAsync(urls);

        var result = await _sut.List(1, 20, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
