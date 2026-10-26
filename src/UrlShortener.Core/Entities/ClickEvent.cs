namespace UrlShortener.Core.Entities;

public class ClickEvent
{
    public long Id { get; set; }
    public long ShortUrlId { get; set; }
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }

    public ShortUrl ShortUrl { get; set; } = null!;
}
