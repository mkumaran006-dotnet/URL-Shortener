namespace UrlShortener.Api.Models.Responses;

public record UrlResponse(
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    string? Title,
    long ClickCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);
