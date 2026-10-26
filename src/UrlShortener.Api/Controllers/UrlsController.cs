using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models.Requests;
using UrlShortener.Api.Models.Responses;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
[Produces("application/json")]
public class UrlsController(IUrlShortenerService svc, IHttpContextAccessor httpCtx) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UrlResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Create([FromBody] CreateUrlRequest req, CancellationToken ct)
    {
        var url = await svc.CreateAsync(req.OriginalUrl, req.CustomAlias, req.Title, req.ExpiresAt, ct: ct);
        var response = ToResponse(url);
        return CreatedAtAction(nameof(GetByCode), new { shortCode = url.ShortCode }, response);
    }

    [HttpGet("{shortCode}")]
    [ProducesResponseType(typeof(UrlResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCode(string shortCode, CancellationToken ct)
    {
        var url = await svc.GetAsync(shortCode, ct);
        return url is null ? NotFound() : Ok(ToResponse(url));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UrlResponse>), 200)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await svc.ListAsync(page, pageSize, ct);
        return Ok(new PagedResponse<UrlResponse>(items.Select(ToResponse).ToList(), page, pageSize, items.Count));
    }

    [HttpPatch("{shortCode}")]
    [ProducesResponseType(typeof(UrlResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string shortCode, [FromBody] UpdateUrlRequest req, CancellationToken ct)
    {
        var url = await svc.UpdateAsync(shortCode, req.Title, req.ExpiresAt, req.IsActive, ct);
        return Ok(ToResponse(url));
    }

    [HttpDelete("{shortCode}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string shortCode, CancellationToken ct)
    {
        await svc.DeleteAsync(shortCode, ct);
        return NoContent();
    }

    private UrlResponse ToResponse(ShortUrl url)
    {
        var req = httpCtx.HttpContext?.Request;
        var baseUrl = req is not null ? $"{req.Scheme}://{req.Host}" : "https://short.example.com";
        return new UrlResponse(
            url.ShortCode,
            $"{baseUrl}/{url.ShortCode}",
            url.OriginalUrl,
            url.Title,
            url.ClickCount,
            url.IsActive,
            url.CreatedAt,
            url.ExpiresAt);
    }
}
