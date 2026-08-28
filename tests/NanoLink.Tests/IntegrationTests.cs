using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NanoLink.Api.Models;
using Xunit;

namespace NanoLink.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient(string? clientIp = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        string ip = clientIp ?? $"10.0.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}";
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOkAndHealthyStatus()
    {
        var client = CreateTestClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task CreateAndRedirect_EndToEndWorkflow_Succeeds()
    {
        var client = CreateTestClient();
        string alias = $"dn10-{Guid.NewGuid():N}"[..12];
        var createRequest = new CreateUrlRequest(
            TargetUrl: "https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview",
            CustomAlias: alias,
            TtlSeconds: 3600
        );

        var createResponse = await client.PostAsJsonAsync("/api/v1/urls", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUrl = await createResponse.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(createdUrl);
        Assert.Equal(alias, createdUrl.ShortCode);

        // Redirect to Target
        var redirectResponse = await client.GetAsync($"/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(createRequest.TargetUrl, redirectResponse.Headers.Location?.ToString());

        // Check URL stats
        var statsResponse = await client.GetAsync($"/api/v1/urls/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var stats = await statsResponse.Content.ReadFromJsonAsync<UrlStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(1, stats.VisitCount);
        Assert.False(stats.IsExpired);

        // Check Global Stats
        var globalStatsResponse = await client.GetAsync("/api/v1/stats");
        Assert.Equal(HttpStatusCode.OK, globalStatsResponse.StatusCode);
        var globalStats = await globalStatsResponse.Content.ReadFromJsonAsync<GlobalStatsResponse>();
        Assert.NotNull(globalStats);
        Assert.True(globalStats.TotalUrls >= 1);
        Assert.True(globalStats.TotalVisits >= 1);

        // Delete URL
        var deleteResponse = await client.DeleteAsync($"/api/v1/urls/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // After deletion, redirect should return 404
        var redirectAfterDelete = await client.GetAsync($"/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.NotFound, redirectAfterDelete.StatusCode);
    }

    [Fact]
    public async Task QrCodeEndpoint_SvgAndPng_ReturnsValidImages()
    {
        var client = CreateTestClient();
        string alias = $"qr-{Guid.NewGuid():N}"[..10];
        var createRequest = new CreateUrlRequest("https://example.com/for-qr", CustomAlias: alias);
        await client.PostAsJsonAsync("/api/v1/urls", createRequest);

        // SVG test
        var svgResp = await client.GetAsync($"/api/v1/urls/{alias}/qrcode?format=svg");
        Assert.Equal(HttpStatusCode.OK, svgResp.StatusCode);
        Assert.Equal("image/svg+xml", svgResp.Content.Headers.ContentType?.MediaType);
        var svgContent = await svgResp.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svgContent);

        // PNG test
        var pngResp = await client.GetAsync($"/api/v1/urls/{alias}/qrcode?format=png&size=128");
        Assert.Equal(HttpStatusCode.OK, pngResp.StatusCode);
        Assert.Equal("image/png", pngResp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task WebhookEndpoints_RegisterAndList_Succeeds()
    {
        var client = CreateTestClient();
        string targetUrl = $"https://my-webhook.site/{Guid.NewGuid():N}";
        var webhookReq = new RegisterWebhookRequest(targetUrl, "secretKey123");
        var postResp = await client.PostAsJsonAsync("/api/v1/webhooks", webhookReq);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var listResp = await client.GetAsync("/api/v1/webhooks");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var subs = await listResp.Content.ReadFromJsonAsync<List<WebhookSubscription>>();
        Assert.NotNull(subs);
        Assert.Contains(subs, s => s.TargetUrl == targetUrl);
    }

    [Fact]
    public async Task PasswordProtectedUrl_RequiresUnlockToAccess()
    {
        var client = CreateTestClient();
        string alias = $"sec-{Guid.NewGuid():N}"[..10];
        var createRequest = new CreateUrlRequest("https://example.com/classified", CustomAlias: alias, Password: "Pass@1234Password");
        var createResp = await client.PostAsJsonAsync("/api/v1/urls", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // Accessing directly without password returns 401
        var redirectResp = await client.GetAsync($"/{alias}");
        Assert.Equal(HttpStatusCode.Unauthorized, redirectResp.StatusCode);

        // Unlock with invalid password returns 401
        var wrongUnlockResp = await client.PostAsJsonAsync($"/api/v1/urls/{alias}/unlock", new UnlockUrlRequest("Wrong"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongUnlockResp.StatusCode);

        // Unlock with correct password returns 200
        var correctUnlockResp = await client.PostAsJsonAsync($"/api/v1/urls/{alias}/unlock", new UnlockUrlRequest("Pass@1234Password"));
        Assert.Equal(HttpStatusCode.OK, correctUnlockResp.StatusCode);
    }

    [Fact]
    public async Task CreateUrl_InvalidRequest_ReturnsBadRequest()
    {
        var client = CreateTestClient();
        var invalidRequest = new CreateUrlRequest("not-a-valid-url");
        var response = await client.PostAsJsonAsync("/api/v1/urls", invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentUrl_ReturnsNotFound()
    {
        var client = CreateTestClient();
        var response = await client.GetAsync("/non-existent-code-12345");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResponseHeaders_ContainRateLimitInfo()
    {
        var client = CreateTestClient();
        var response = await client.GetAsync("/api/v1/stats");
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
    }
}
