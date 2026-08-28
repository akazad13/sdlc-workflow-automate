using Microsoft.EntityFrameworkCore;
using NanoLink.Api.Models;

namespace NanoLink.Api.Storage;

public class SqliteUrlRepository : IUrlRepository
{
    private readonly IDbContextFactory<NanoLinkDbContext> _contextFactory;

    public SqliteUrlRepository(IDbContextFactory<NanoLinkDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<UrlRecord?> GetAsync(string shortCode, bool recordVisit = true, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var record = await db.Urls.FirstOrDefaultAsync(u => u.ShortCode == shortCode, ct);

        if (record == null || record.IsExpired)
        {
            return null;
        }

        if (recordVisit)
        {
            record.VisitCount++;
            record.LastAccessedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return record;
    }

    public async Task<UrlRecord?> GetStatsAsync(string shortCode, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Urls.AsNoTracking().FirstOrDefaultAsync(u => u.ShortCode == shortCode, ct);
    }

    public async Task<bool> CreateAsync(UrlRecord record, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var exists = await db.Urls.AnyAsync(u => u.ShortCode == record.ShortCode, ct);
        if (exists)
        {
            return false;
        }

        db.Urls.Add(record);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string shortCode, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var record = await db.Urls.FirstOrDefaultAsync(u => u.ShortCode == shortCode, ct);
        if (record == null)
        {
            return false;
        }

        db.Urls.Remove(record);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var expiredList = await db.Urls.Where(u => u.ExpiresAtUtc.HasValue && u.ExpiresAtUtc.Value < now).ToListAsync(ct);

        if (expiredList.Count == 0)
        {
            return 0;
        }

        db.Urls.RemoveRange(expiredList);
        await db.SaveChangesAsync(ct);
        return expiredList.Count;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Urls.CountAsync(ct);
    }

    public async Task<GlobalStatsResponse> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        int total = await db.Urls.CountAsync(ct);
        int expired = await db.Urls.CountAsync(u => u.ExpiresAtUtc.HasValue && u.ExpiresAtUtc.Value < now, ct);
        int active = total - expired;
        long totalVisits = await db.Urls.SumAsync(u => (long)u.VisitCount, ct);

        return new GlobalStatsResponse(total, active, expired, totalVisits);
    }
}
