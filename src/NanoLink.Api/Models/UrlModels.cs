namespace NanoLink.Api.Models;

public record CreateUrlRequest(
    string TargetUrl,
    string? CustomAlias = null,
    int? TtlSeconds = null
);

public record UrlResponse(
    string ShortCode,
    string ShortUrl,
    string TargetUrl,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc
);

public record UrlStatsResponse(
    string ShortCode,
    string TargetUrl,
    int VisitCount,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? LastAccessedUtc,
    bool IsExpired
);

public record GlobalStatsResponse(
    int TotalUrls,
    int ActiveUrls,
    int ExpiredUrls,
    long TotalVisits
);

public class UrlRecord
{
    public required string ShortCode { get; set; }
    public required string TargetUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastAccessedUtc { get; set; }
    public int VisitCount { get; set; }

    public bool IsExpired => ExpiresAtUtc.HasValue && DateTime.UtcNow > ExpiresAtUtc.Value;
}