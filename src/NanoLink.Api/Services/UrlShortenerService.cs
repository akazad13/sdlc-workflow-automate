using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NanoLink.Api.Models;
using NanoLink.Api.Storage;

namespace NanoLink.Api.Services;

public partial class UrlShortenerService : IUrlShortenerService
{
    private readonly IUrlRepository _repository;
    private const string Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int DefaultCodeLength = 6;
    private const int MaxCollisionRetries = 5;

    public UrlShortenerService(IUrlRepository repository)
    {
        _repository = repository;
    }

    public async Task<(bool Success, string? Error, UrlResponse? Result)> ShortenUrlAsync(
        CreateUrlRequest request,
        string baseUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUrl))
        {
            return (false, "Target URL cannot be empty.", null);
        }

        if (request.TargetUrl.Length > 2048)
        {
            return (false, "Target URL exceeds maximum length of 2048 characters.", null);
        }

        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return (false, "Invalid URL. Only HTTP and HTTPS schemes are supported.", null);
        }

        if (request.TtlSeconds.HasValue && request.TtlSeconds.Value <= 0)
        {
            return (false, "TTL must be greater than 0 seconds.", null);
        }

        string shortCode;
        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            var alias = request.CustomAlias.Trim();
            if (!CustomAliasRegex().IsMatch(alias))
            {
                return (false, "Custom alias must be 3-32 characters long and contain only alphanumeric characters, underscores, or hyphens.", null);
            }

            var existing = await _repository.GetStatsAsync(alias, ct);
            if (existing != null && !existing.IsExpired)
            {
                return (false, $"Custom alias '{alias}' is already in use.", null);
            }

            shortCode = alias;
        }
        else
        {
            shortCode = await GenerateUniqueCodeAsync(ct);
        }

        DateTime? expiresAt = request.TtlSeconds.HasValue
            ? DateTime.UtcNow.AddSeconds(request.TtlSeconds.Value)
            : null;

        var record = new UrlRecord
        {
            ShortCode = shortCode,
            TargetUrl = parsedUri.ToString(),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt,
            VisitCount = 0
        };

        var created = await _repository.CreateAsync(record, ct);
        if (!created)
        {
            return (false, "Conflict detected. Please retry.", null);
        }

        string formattedBaseUrl = baseUrl.TrimEnd('/');
        var response = new UrlResponse(
            ShortCode: record.ShortCode,
            ShortUrl: $"{formattedBaseUrl}/{record.ShortCode}",
            TargetUrl: record.TargetUrl,
            CreatedAtUtc: record.CreatedAtUtc,
            ExpiresAtUtc: record.ExpiresAtUtc
        );

        return (true, null, response);
    }

    public async Task<UrlRecord?> GetTargetAsync(string shortCode, CancellationToken ct = default)
    {
        return await _repository.GetAsync(shortCode, recordVisit: true, ct);
    }

    public async Task<UrlStatsResponse?> GetStatsAsync(string shortCode, CancellationToken ct = default)
    {
        var record = await _repository.GetStatsAsync(shortCode, ct);
        if (record == null) return null;

        return new UrlStatsResponse(
            ShortCode: record.ShortCode,
            TargetUrl: record.TargetUrl,
            VisitCount: record.VisitCount,
            CreatedAtUtc: record.CreatedAtUtc,
            ExpiresAtUtc: record.ExpiresAtUtc,
            LastAccessedUtc: record.LastAccessedUtc,
            IsExpired: record.IsExpired
        );
    }

    public async Task<GlobalStatsResponse> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        return await _repository.GetGlobalStatsAsync(ct);
    }

    public async Task<bool> DeleteUrlAsync(string shortCode, CancellationToken ct = default)
    {
        return await _repository.DeleteAsync(shortCode, ct);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (int i = 0; i < MaxCollisionRetries; i++)
        {
            string code = GenerateRandomCode(DefaultCodeLength);
            var existing = await _repository.GetStatsAsync(code, ct);
            if (existing == null || existing.IsExpired)
            {
                return code;
            }
        }

        // If collision after retries, use slightly longer code
        return GenerateRandomCode(DefaultCodeLength + 2);
    }

    private static string GenerateRandomCode(int length)
    {
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);
        Span<char> code = stackalloc char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = Base62Chars[randomBytes[i] % Base62Chars.Length];
        }
        return new string(code);
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]{3,32}$")]
    private static partial Regex CustomAliasRegex();
}
