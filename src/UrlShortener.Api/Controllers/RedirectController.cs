using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController(IUrlShortenerService svc) : ControllerBase
{
    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> Redirect(string shortCode, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var referrer = Request.Headers.Referer.ToString();

        var url = await svc.ResolveAsync(shortCode, ip, ua, referrer, ct);
        return RedirectPermanent(url.OriginalUrl);
    }
}
