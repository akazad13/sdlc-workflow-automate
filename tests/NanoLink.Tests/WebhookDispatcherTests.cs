using NanoLink.Api.Models;
using NanoLink.Api.Services;
using Xunit;

namespace NanoLink.Tests;

public class WebhookDispatcherTests
{
    private readonly WebhookDispatcherService _dispatcher = new();

    [Fact]
    public async Task RegisterWebhook_StoresSubscriptionSuccessfully()
    {
        var request = new RegisterWebhookRequest(
            TargetUrl: "https://example.com/webhook",
            SecretKey: "supersecret123"
        );

        var sub = await _dispatcher.RegisterWebhookAsync(request);

        Assert.NotNull(sub);
        Assert.Equal("https://example.com/webhook", sub.TargetUrl);
        Assert.Single(_dispatcher.GetSubscriptions());
    }

    [Fact]
    public void ComputeHmacSignature_ProducesValidDeterministicHash()
    {
        string payload = "{\"event\":\"url.visited\",\"code\":\"test1\"}";
        string secret = "my-secret-key";

        string sig1 = _dispatcher.ComputeHmacSignature(payload, secret);
        string sig2 = _dispatcher.ComputeHmacSignature(payload, secret);

        Assert.False(string.IsNullOrWhiteSpace(sig1));
        Assert.Equal(sig1, sig2);
        Assert.Equal(64, sig1.Length); // SHA-256 hex string length
    }

    [Fact]
    public async Task PublishEvent_QueuesEventWithoutThrowing()
    {
        var exception = await Record.ExceptionAsync(() => 
            _dispatcher.PublishEventAsync("url.visited", "code123", "https://example.com"));

        Assert.Null(exception);
    }
}