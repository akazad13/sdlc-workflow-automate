using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NanoLink.Api.Middleware;
using NanoLink.Api.Models;
using NanoLink.Api.Services;
using NanoLink.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure Storage Provider (InMemory vs Sqlite)
string storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "Sqlite";
string connectionString = builder.Configuration.GetValue<string>("Storage:ConnectionString") ?? "Data Source=nanolink.db";

if (storageProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContextFactory<NanoLinkDbContext>(options =>
        options.UseSqlite(connectionString));
    builder.Services.AddSingleton<IUrlRepository, SqliteUrlRepository>();
}
else
{
    builder.Services.AddSingleton<IUrlRepository, InMemoryUrlRepository>();
}

// Register Domain, Security, and Feature Services
builder.Services.AddSingleton<ITokenBucketRateLimiter>(_ => new TokenBucketRateLimiter(capacity: 10, refillTokensPerMinute: 10));
builder.Services.AddSingleton<IPasswordProtectionService, PasswordProtectionService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddSingleton<IWebhookDispatcher, WebhookDispatcherService>();
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

// Register Background Services
builder.Services.AddHostedService<UrlCleanupBackgroundService>();

var app = builder.Build();

// Ensure SQLite Database is initialized if using Sqlite
if (storageProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    var factory = app.Services.GetRequiredService<IDbContextFactory<NanoLinkDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RateLimitingMiddleware>();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "NanoLink.Api",
    version = "1.5.0",
    storageProvider,
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithTags("System");

// Global System Statistics
app.MapGet("/api/v1/stats", async (
    [FromServices] IUrlShortenerService shortenerService,
    CancellationToken ct) =>
{
    var stats = await shortenerService.GetGlobalStatsAsync(ct);
    return Results.Ok(stats);
})
.WithName("GetGlobalStats")
.WithTags("Analytics");

// Create Short URL
app.MapPost("/api/v1/urls", async (
    [FromBody] CreateUrlRequest request,
    [FromServices] IUrlShortenerService shortenerService,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var (success, error, result) = await shortenerService.ShortenUrlAsync(request, baseUrl, ct);

    if (!success)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Created($"/api/v1/urls/{result!.ShortCode}", result);
})
.WithName("CreateShortUrl")
.WithTags("Urls");

// Get URL Statistics
app.MapGet("/api/v1/urls/{shortCode}", async (
    string shortCode,
    [FromServices] IUrlShortenerService shortenerService,
    CancellationToken ct) =>
{
    var stats = await shortenerService.GetStatsAsync(shortCode, ct);
    if (stats == null)
    {
        return Results.NotFound(new { error = $"Short URL code '{shortCode}' was not found." });
    }

    return Results.Ok(stats);
})
.WithName("GetUrlStats")
.WithTags("Urls");

// Delete Short URL
app.MapDelete("/api/v1/urls/{shortCode}", async (
    string shortCode,
    [FromServices] IUrlShortenerService shortenerService,
    CancellationToken ct) =>
{
    var deleted = await shortenerService.DeleteUrlAsync(shortCode, ct);
    if (!deleted)
    {
        return Results.NotFound(new { error = $"Short URL code '{shortCode}' was not found." });
    }

    return Results.NoContent();
})
.WithName("DeleteUrl")
.WithTags("Urls");

// Unlock Password-Protected URL
app.MapPost("/api/v1/urls/{shortCode}/unlock", async (
    string shortCode,
    [FromBody] UnlockUrlRequest request,
    [FromServices] IUrlShortenerService shortenerService,
    [FromServices] IWebhookDispatcher webhookDispatcher,
    CancellationToken ct) =>
{
    var (success, error, targetUrl) = await shortenerService.VerifyAndUnlockAsync(shortCode, request.Password, ct);
    if (!success)
    {
        return Results.Unauthorized();
    }

    await webhookDispatcher.PublishEventAsync("url.visited", shortCode, targetUrl!, ct);
    return Results.Ok(new { shortCode, targetUrl });
})
.WithName("UnlockUrl")
.WithTags("Urls");

// Generate QR Code for Short URL
app.MapGet("/api/v1/urls/{shortCode}/qrcode", async (
    string shortCode,
    [FromQuery] string? format,
    [FromQuery] int? size,
    [FromServices] IUrlShortenerService shortenerService,
    [FromServices] IQrCodeService qrCodeService,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    var stats = await shortenerService.GetStatsAsync(shortCode, ct);
    if (stats == null || stats.IsExpired)
    {
        return Results.NotFound(new { error = $"Short URL '{shortCode}' was not found or expired." });
    }

    string fullShortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{shortCode}";
    int imageSize = Math.Clamp(size ?? 256, 64, 1024);

    httpContext.Response.Headers.CacheControl = "public, max-age=86400";

    if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
    {
        byte[] pngBytes = qrCodeService.GeneratePngMockQrCode(fullShortUrl, imageSize);
        return Results.File(pngBytes, "image/png");
    }

    string svg = qrCodeService.GenerateSvgQrCode(fullShortUrl, imageSize);
    return Results.Content(svg, "image/svg+xml");
})
.WithName("GetQrCode")
.WithTags("QrCode");

// Register Webhook
app.MapPost("/api/v1/webhooks", async (
    [FromBody] RegisterWebhookRequest request,
    [FromServices] IWebhookDispatcher webhookDispatcher,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.TargetUrl) || string.IsNullOrWhiteSpace(request.SecretKey))
    {
        return Results.BadRequest(new { error = "TargetUrl and SecretKey are required." });
    }

    var sub = await webhookDispatcher.RegisterWebhookAsync(request, ct);
    return Results.Created($"/api/v1/webhooks/{sub.Id}", sub);
})
.WithName("RegisterWebhook")
.WithTags("Webhooks");

// List Webhooks
app.MapGet("/api/v1/webhooks", (
    [FromServices] IWebhookDispatcher webhookDispatcher) =>
{
    var subs = webhookDispatcher.GetSubscriptions();
    return Results.Ok(subs);
})
.WithName("ListWebhooks")
.WithTags("Webhooks");

// Redirect Short URL
app.MapGet("/{shortCode}", async (
    string shortCode,
    [FromServices] IUrlShortenerService shortenerService,
    [FromServices] IWebhookDispatcher webhookDispatcher,
    CancellationToken ct) =>
{
    var stats = await shortenerService.GetStatsAsync(shortCode, ct);
    if (stats == null || stats.IsExpired)
    {
        return Results.NotFound(new { error = $"Short URL code '{shortCode}' not found or expired." });
    }

    if (stats.IsExhausted)
    {
        return Results.StatusCode(StatusCodes.Status410Gone);
    }

    if (stats.IsPasswordProtected)
    {
        return Results.Unauthorized();
    }

    var record = await shortenerService.GetTargetAsync(shortCode, ct);
    if (record == null)
    {
        return Results.NotFound(new { error = $"Short URL code '{shortCode}' not found." });
    }

    await webhookDispatcher.PublishEventAsync("url.visited", record.ShortCode, record.TargetUrl, ct);

    return Results.Redirect(record.TargetUrl, permanent: false);
})
.WithName("RedirectUrl")
.WithTags("Redirect");

app.Run();

public partial class Program { }
