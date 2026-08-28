namespace NanoLink.Api.Models;

public record CreateUrlRequest(
    string TargetUrl,
    string? CustomAlias = null,
    int? TtlSeconds = null,
    string? Password = null,
    int? MaxVisits = null
);

public record UrlResponse(
    string ShortCode,
    string ShortUrl,
    string TargetUrl,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsPasswordProtected = false,
    int? MaxVisits = null
);

public record UrlStatsResponse(
    string ShortCode,
    string TargetUrl,
    int VisitCount,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? LastAccessedUtc,
    bool IsExpired,
    bool IsPasswordProtected = false,
    int? MaxVisits = null,
    bool IsExhausted = false
);

public record GlobalStatsResponse(
    int TotalUrls,
    int ActiveUrls,
    int ExpiredUrls,
    long TotalVisits
);

public record UnlockUrlRequest(
    string Password
);

public class UrlRecord
{
    public required string ShortCode { get; set; }
    public required string TargetUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastAccessedUtc { get; set; }
    public int VisitCount { get; set; }

    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public int? MaxVisits { get; set; }

    public bool IsPasswordProtected => !string.IsNullOrEmpty(PasswordHash);
    public bool IsExpired => ExpiresAtUtc.HasValue && DateTime.UtcNow > ExpiresAtUtc.Value;
    public bool IsExhausted => MaxVisits.HasValue && VisitCount >= MaxVisits.Value;
}