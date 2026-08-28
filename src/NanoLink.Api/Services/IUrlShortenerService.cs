using NanoLink.Api.Models;

namespace NanoLink.Api.Services;

public interface IUrlShortenerService
{
    Task<(bool Success, string? Error, UrlResponse? Result)> ShortenUrlAsync(CreateUrlRequest request, string baseUrl, CancellationToken ct = default);
    Task<UrlRecord?> GetTargetAsync(string shortCode, CancellationToken ct = default);
    Task<UrlStatsResponse?> GetStatsAsync(string shortCode, CancellationToken ct = default);
    Task<GlobalStatsResponse> GetGlobalStatsAsync(CancellationToken ct = default);
    Task<bool> DeleteUrlAsync(string shortCode, CancellationToken ct = default);
}
