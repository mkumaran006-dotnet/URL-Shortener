using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models.Responses;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls/{shortCode}/analytics")]
[Produces("application/json")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsSummaryResponse), 200)]
    public async Task<IActionResult> Summary(string shortCode, CancellationToken ct)
    {
        var s = await analytics.GetSummaryAsync(shortCode, ct);
        return Ok(new AnalyticsSummaryResponse(s.ShortCode, s.OriginalUrl, s.TotalClicks, s.CreatedAt, s.ExpiresAt, s.IsActive));
    }

    [HttpGet("daily")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyClickResponse>), 200)]
    public async Task<IActionResult> Daily(
        string shortCode,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to ?? DateTime.UtcNow;
        var stats = await analytics.GetDailyClicksAsync(shortCode, f, t, ct);
        return Ok(stats.Select(s => new DailyClickResponse(s.Date, s.Count)));
    }

    [HttpGet("referrers")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferrerResponse>), 200)]
    public async Task<IActionResult> Referrers(string shortCode, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var stats = await analytics.GetTopReferrersAsync(shortCode, top, ct);
        return Ok(stats.Select(s => new ReferrerResponse(s.Referrer, s.Count)));
    }

    [HttpGet("countries")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryResponse>), 200)]
    public async Task<IActionResult> Countries(string shortCode, CancellationToken ct)
    {
        var stats = await analytics.GetCountryBreakdownAsync(shortCode, ct);
        return Ok(stats.Select(s => new CountryResponse(s.Country, s.Count)));
    }
}
