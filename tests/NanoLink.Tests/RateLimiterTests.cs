using NanoLink.Api.Services;
using Xunit;

namespace NanoLink.Tests;

public class RateLimiterTests
{
    [Fact]
    public void TryAcquire_WithinCapacity_AllowsAllRequests()
    {
        // Arrange
        var limiter = new TokenBucketRateLimiter(capacity: 5, refillTokensPerMinute: 60);
        string clientIp = "192.168.1.100";

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            bool acquired = limiter.TryAcquire(clientIp, out int remaining, out var retryAfter);
            Assert.True(acquired, $"Request {i + 1} should be permitted");
            Assert.Equal(4 - i, remaining);
            Assert.Equal(TimeSpan.Zero, retryAfter);
        }
    }

    [Fact]
    public void TryAcquire_ExceedsCapacity_RejectsSubsequentRequests()
    {
        // Arrange
        var limiter = new TokenBucketRateLimiter(capacity: 3, refillTokensPerMinute: 6);
        string clientIp = "10.0.0.1";

        // Consume all 3 tokens
        for (int i = 0; i < 3; i++)
        {
            Assert.True(limiter.TryAcquire(clientIp, out _, out _));
        }

        // 4th request should fail
        bool acquired = limiter.TryAcquire(clientIp, out int remaining, out var retryAfter);

        // Assert
        Assert.False(acquired);
        Assert.Equal(0, remaining);
        Assert.True(retryAfter.TotalSeconds > 0);
    }

    [Fact]
    public void TryAcquire_IsolatedPerClientIp()
    {
        // Arrange
        var limiter = new TokenBucketRateLimiter(capacity: 2, refillTokensPerMinute: 10);
        string ip1 = "10.0.0.1";
        string ip2 = "10.0.0.2";

        // Exhaust IP 1
        Assert.True(limiter.TryAcquire(ip1, out _, out _));
        Assert.True(limiter.TryAcquire(ip1, out _, out _));
        Assert.False(limiter.TryAcquire(ip1, out _, out _));

        // IP 2 should still have full quota
        Assert.True(limiter.TryAcquire(ip2, out int remainingIp2, out _));
        Assert.Equal(1, remainingIp2);
    }

    [Fact]
    public void Reset_ClearsAllBuckets()
    {
        // Arrange
        var limiter = new TokenBucketRateLimiter(capacity: 1, refillTokensPerMinute: 10);
        string ip = "172.16.0.1";

        Assert.True(limiter.TryAcquire(ip, out _, out _));
        Assert.False(limiter.TryAcquire(ip, out _, out _));

        // Act
        limiter.Reset();

        // Assert
        Assert.True(limiter.TryAcquire(ip, out _, out _));
    }
}
