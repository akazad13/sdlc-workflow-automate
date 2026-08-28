namespace NanoLink.Api.Models;

public record RegisterWebhookRequest(
    string TargetUrl,
    string SecretKey,
    List<string>? SubscribedEvents = null
);

public record WebhookSubscription(
    string Id,
    string TargetUrl,
    string SecretKey,
    List<string> SubscribedEvents,
    DateTime CreatedAtUtc
);

public record WebhookEventPayload(
    string EventType,
    string ShortCode,
    string TargetUrl,
    DateTime TimestampUtc
);