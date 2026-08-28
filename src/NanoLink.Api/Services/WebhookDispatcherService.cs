using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using NanoLink.Api.Models;

namespace NanoLink.Api.Services;

public interface IWebhookDispatcher
{
    Task<WebhookSubscription> RegisterWebhookAsync(RegisterWebhookRequest request, CancellationToken ct = default);
    Task PublishEventAsync(string eventType, string shortCode, string targetUrl, CancellationToken ct = default);
    IReadOnlyList<WebhookSubscription> GetSubscriptions();
    string ComputeHmacSignature(string payload, string secret);
}

public class WebhookDispatcherService : IWebhookDispatcher
{
    private readonly ConcurrentDictionary<string, WebhookSubscription> _subscriptions = new();
    private readonly Channel<WebhookEventPayload> _eventChannel = Channel.CreateUnbounded<WebhookEventPayload>();

    public Task<WebhookSubscription> RegisterWebhookAsync(RegisterWebhookRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var sub = new WebhookSubscription(
            Id: id,
            TargetUrl: request.TargetUrl,
            SecretKey: request.SecretKey,
            SubscribedEvents: request.SubscribedEvents ?? ["url.visited", "url.expired"],
            CreatedAtUtc: DateTime.UtcNow
        );

        _subscriptions.TryAdd(id, sub);
        return Task.FromResult(sub);
    }

    public async Task PublishEventAsync(string eventType, string shortCode, string targetUrl, CancellationToken ct = default)
    {
        var payload = new WebhookEventPayload(eventType, shortCode, targetUrl, DateTime.UtcNow);
        await _eventChannel.Writer.WriteAsync(payload, ct);
    }

    public IReadOnlyList<WebhookSubscription> GetSubscriptions()
    {
        return _subscriptions.Values.ToList();
    }

    public string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}