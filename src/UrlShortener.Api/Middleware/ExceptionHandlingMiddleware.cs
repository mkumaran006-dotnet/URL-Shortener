using System.Text.Json;
using UrlShortener.Core.Exceptions;

namespace UrlShortener.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, title) = ex switch
        {
            UrlNotFoundException e    => (404, "url_not_found",     e.Message),
            DuplicateAliasException e => (409, "duplicate_alias",   e.Message),
            UrlExpiredException e     => (410, "url_expired",       e.Message),
            ArgumentException e       => (400, "invalid_argument",  e.Message),
            _                         => (500, "internal_error",    "An unexpected error occurred.")
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        var body = JsonSerializer.Serialize(new
        {
            type    = $"https://urlshortener.example.com/errors/{code}",
            title,
            status,
            traceId = ctx.TraceIdentifier
        });

        return ctx.Response.WriteAsync(body);
    }
}
