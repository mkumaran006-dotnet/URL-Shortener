namespace UrlShortener.Core.Exceptions;

public class UrlNotFoundException(string shortCode)
    : Exception($"Short URL '{shortCode}' was not found.")
{
    public string ShortCode { get; } = shortCode;
}
