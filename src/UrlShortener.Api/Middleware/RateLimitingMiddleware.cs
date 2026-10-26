using System.Collections.Concurrent;

namespace UrlShortener.Api.Middleware;

/// <summary>
/// Simple sliding-window rate limiter per IP address.
/// Production deployments should use a distributed rate limiter backed by Redis.
/// </summary>
public class RateLimitingMiddleware(RequestDelegate next, IConfiguration config, ILogger<RateLimitingMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();

    private readonly int _limit = config.GetValue("RateLimit:RequestsPerWindow", 100);
    private readonly TimeSpan _window = TimeSpan.FromSeconds(config.GetValue("RateLimit:WindowSeconds", 60));

    public async Task InvokeAsync(HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var queue = _windows.GetOrAdd(ip, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window)
                queue.Dequeue();

            if (queue.Count >= _limit)
            {
                logger.LogWarning("Rate limit exceeded for IP {Ip}", ip);
                ctx.Response.StatusCode = 429;
                ctx.Response.Headers.RetryAfter = _window.TotalSeconds.ToString("0");
                return;
            }

            queue.Enqueue(now);
        }

        await next(ctx);
    }
}
