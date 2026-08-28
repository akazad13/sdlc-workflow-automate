using System.Collections.Concurrent;

namespace NanoLink.Api.Services;

public class TokenBucketRateLimiter : ITokenBucketRateLimiter
{
    private readonly double _capacity;
    private readonly double _refillRatePerSecond; // tokens per second
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

    public TokenBucketRateLimiter(int capacity = 10, int refillTokensPerMinute = 10)
    {
        _capacity = capacity;
        _refillRatePerSecond = (double)refillTokensPerMinute / 60.0;
    }

    public bool TryAcquire(string clientIp, out int remainingTokens, out TimeSpan retryAfter)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            clientIp = "anonymous";
        }

        var bucket = _buckets.GetOrAdd(clientIp, _ => new TokenBucket(_capacity, DateTime.UtcNow));

        lock (bucket)
        {
            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - bucket.LastRefillUtc).TotalSeconds;

            if (elapsedSeconds > 0)
            {
                bucket.Tokens = Math.Min(_capacity, bucket.Tokens + (elapsedSeconds * _refillRatePerSecond));
                bucket.LastRefillUtc = now;
            }

            if (bucket.Tokens >= 1.0)
            {
                bucket.Tokens -= 1.0;
                remainingTokens = (int)Math.Floor(bucket.Tokens);
                retryAfter = TimeSpan.Zero;
                return true;
            }
            else
            {
                remainingTokens = 0;
                double tokensNeeded = 1.0 - bucket.Tokens;
                double secondsToWait = tokensNeeded / _refillRatePerSecond;
                retryAfter = TimeSpan.FromSeconds(Math.Ceiling(secondsToWait));
                return false;
            }
        }
    }

    public void Reset()
    {
        _buckets.Clear();
    }

    private class TokenBucket
    {
        public double Tokens { get; set; }
        public DateTime LastRefillUtc { get; set; }

        public TokenBucket(double initialTokens, DateTime lastRefillUtc)
        {
            Tokens = initialTokens;
            LastRefillUtc = lastRefillUtc;
        }
    }
}
