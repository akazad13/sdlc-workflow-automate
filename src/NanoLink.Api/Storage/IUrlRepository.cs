using NanoLink.Api.Models;

namespace NanoLink.Api.Storage;

public interface IUrlRepository
{
    Task<UrlRecord?> GetAsync(string shortCode, bool recordVisit = true, CancellationToken ct = default);
    Task<UrlRecord?> GetStatsAsync(string shortCode, CancellationToken ct = default);
    Task<bool> CreateAsync(UrlRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(string shortCode, CancellationToken ct = default);
    Task<int> CleanupExpiredAsync(CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<GlobalStatsResponse> GetGlobalStatsAsync(CancellationToken ct = default);
}