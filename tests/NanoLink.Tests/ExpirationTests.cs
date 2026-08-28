using NanoLink.Api.Models;
using NanoLink.Api.Services;
using NanoLink.Api.Storage;
using Xunit;

namespace NanoLink.Tests;

public class ExpirationTests
{
    private readonly IUrlRepository _repository;
    private readonly IPasswordProtectionService _passwordService;
    private readonly UrlShortenerService _service;

    public ExpirationTests()
    {
        _repository = new InMemoryUrlRepository();
        _passwordService = new PasswordProtectionService();
        _service = new UrlShortenerService(_repository, _passwordService);
    }

    [Fact]
    public async Task GetTarget_ExpiredUrl_ReturnsNull()
    {
        // Arrange
        var expiredRecord = new UrlRecord
        {
            ShortCode = "exp123",
            TargetUrl = "https://example.com/expired",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) // Expired 1 minute ago
        };
        await _repository.CreateAsync(expiredRecord);

        // Act
        var result = await _service.GetTargetAsync("exp123");
        var stats = await _service.GetStatsAsync("exp123");

        // Assert
        Assert.Null(result);
        Assert.NotNull(stats);
        Assert.True(stats.IsExpired);
    }

    [Fact]
    public async Task CleanupExpired_RemovesOnlyExpiredUrls()
    {
        // Arrange
        var activeRecord = new UrlRecord
        {
            ShortCode = "active1",
            TargetUrl = "https://example.com/active",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        var expiredRecord1 = new UrlRecord
        {
            ShortCode = "expired1",
            TargetUrl = "https://example.com/exp1",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var expiredRecord2 = new UrlRecord
        {
            ShortCode = "expired2",
            TargetUrl = "https://example.com/exp2",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        await _repository.CreateAsync(activeRecord);
        await _repository.CreateAsync(expiredRecord1);
        await _repository.CreateAsync(expiredRecord2);

        // Act
        int cleanedCount = await _repository.CleanupExpiredAsync();
        var remainingCount = await _repository.GetTotalCountAsync();
        var activeFetched = await _repository.GetAsync("active1");
        var expiredFetched1 = await _repository.GetStatsAsync("expired1");

        // Assert
        Assert.Equal(2, cleanedCount);
        Assert.Equal(1, remainingCount);
        Assert.NotNull(activeFetched);
        Assert.Null(expiredFetched1);
    }
}
