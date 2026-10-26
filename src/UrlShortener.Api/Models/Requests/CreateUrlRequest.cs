using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models.Requests;

public class CreateUrlRequest
{
    [Required]
    [Url]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Alias may only contain letters, digits, hyphens and underscores.")]
    public string? CustomAlias { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class UpdateUrlRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool? IsActive { get; set; }
}
