namespace UrlShortener.Core.Exceptions;

public class UrlExpiredException(string shortCode, DateTime expiredAt)
    : Exception($"Short URL '{shortCode}' expired at {expiredAt:u}.")
{
    public string ShortCode { get; } = shortCode;
    public DateTime ExpiredAt { get; } = expiredAt;
}
