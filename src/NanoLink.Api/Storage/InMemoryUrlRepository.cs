using System.Collections.Concurrent;
using NanoLink.Api.Models;

namespace NanoLink.Api.Storage;

public class InMemoryUrlRepository : IUrlRepository
{
    private readonly ConcurrentDictionary<string, UrlRecord> _urls = new(StringComparer.OrdinalIgnoreCase);

    public Task<UrlRecord?> GetAsync(string shortCode, bool recordVisit = true, CancellationToken ct = default)
    {
        if (!_urls.TryGetValue(shortCode, out var record))
        {
            return Task.FromResult<UrlRecord?>(null);
        }

        if (record.IsExpired)
        {
            return Task.FromResult<UrlRecord?>(null);
        }

        if (recordVisit)
        {
            lock (record)
            {
                record.VisitCount++;
                record.LastAccessedUtc = DateTime.UtcNow;
            }
        }

        return Task.FromResult<UrlRecord?>(record);
    }

    public Task<UrlRecord?> GetStatsAsync(string shortCode, CancellationToken ct = default)
    {
        _urls.TryGetValue(shortCode, out var record);
        return Task.FromResult(record);
    }

    public Task<bool> CreateAsync(UrlRecord record, CancellationToken ct = default)
    {
        var added = _urls.TryAdd(record.ShortCode, record);
        return Task.FromResult(added);
    }

    public Task<bool> DeleteAsync(string shortCode, CancellationToken ct = default)
    {
        var removed = _urls.TryRemove(shortCode, out _);
        return Task.FromResult(removed);
    }

    public Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        var expiredKeys = _urls.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
        int removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_urls.TryRemove(key, out _))
            {
                removedCount++;
            }
        }
        return Task.FromResult(removedCount);
    }

    public Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_urls.Count);
    }

    public Task<GlobalStatsResponse> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        int total = _urls.Count;
        int expired = _urls.Values.Count(r => r.IsExpired);
        int active = total - expired;
        long totalVisits = _urls.Values.Sum(r => (long)r.VisitCount);

        var stats = new GlobalStatsResponse(total, active, expired, totalVisits);
        return Task.FromResult(stats);
    }
}