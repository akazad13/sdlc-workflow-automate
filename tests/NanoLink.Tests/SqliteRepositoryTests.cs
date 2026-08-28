using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NanoLink.Api.Models;
using NanoLink.Api.Storage;
using Xunit;

namespace NanoLink.Tests;

public class SqliteRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<NanoLinkDbContext> _contextFactory;
    private readonly SqliteUrlRepository _repository;

    public SqliteRepositoryTests()
    {
        // Use in-memory SQLite connection for isolated, fast unit testing
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NanoLinkDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var initialContext = new NanoLinkDbContext(options))
        {
            initialContext.Database.EnsureCreated();
        }

        _contextFactory = new TestDbContextFactory(options);
        _repository = new SqliteUrlRepository(_contextFactory);
    }

    [Fact]
    public async Task CreateAndGet_SqliteStorage_Succeeds()
    {
        var record = new UrlRecord
        {
            ShortCode = "sqlite-test",
            TargetUrl = "https://sqlite.org",
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(record);
        var retrieved = await _repository.GetAsync("sqlite-test");

        Assert.True(created);
        Assert.NotNull(retrieved);
        Assert.Equal("https://sqlite.org", retrieved.TargetUrl);
        Assert.Equal(1, retrieved.VisitCount);
    }

    [Fact]
    public async Task CleanupExpired_SqliteStorage_PurgesExpiredEntries()
    {
        var active = new UrlRecord
        {
            ShortCode = "sql-active",
            TargetUrl = "https://example.com/active",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        var expired = new UrlRecord
        {
            ShortCode = "sql-expired",
            TargetUrl = "https://example.com/expired",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        await _repository.CreateAsync(active);
        await _repository.CreateAsync(expired);

        int cleaned = await _repository.CleanupExpiredAsync();
        var stats = await _repository.GetGlobalStatsAsync();

        Assert.Equal(1, cleaned);
        Assert.Equal(1, stats.TotalUrls);
        Assert.Equal(1, stats.ActiveUrls);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private class TestDbContextFactory : IDbContextFactory<NanoLinkDbContext>
    {
        private readonly DbContextOptions<NanoLinkDbContext> _options;

        public TestDbContextFactory(DbContextOptions<NanoLinkDbContext> options)
        {
            _options = options;
        }

        public NanoLinkDbContext CreateDbContext()
        {
            return new NanoLinkDbContext(_options);
        }
    }
}
