namespace NanoLink.Api.Services;

public interface ITokenBucketRateLimiter
{
    bool TryAcquire(string clientIp, out int remainingTokens, out TimeSpan retryAfter);
    void Reset();
}
