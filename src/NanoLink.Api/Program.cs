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

// Register Domain & Rate Limiting Services
builder.Services.AddSingleton<ITokenBucketRateLimiter>(_ => new TokenBucketRateLimiter(capacity: 10, refillTokensPerMinute: 10));
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
    version = "1.2.0",
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

// Redirect Short URL
app.MapGet("/{shortCode}", async (
    string shortCode,
    [FromServices] IUrlShortenerService shortenerService,
    CancellationToken ct) =>
{
    var record = await shortenerService.GetTargetAsync(shortCode, ct);
    if (record == null)
    {
        return Results.NotFound(new { error = $"Short URL code '{shortCode}' not found or expired." });
    }

    return Results.Redirect(record.TargetUrl, permanent: false);
})
.WithName("RedirectUrl")
.WithTags("Redirect");

app.Run();

public partial class Program { }
