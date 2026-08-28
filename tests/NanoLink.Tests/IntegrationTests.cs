using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NanoLink.Api.Models;
using Xunit;

namespace NanoLink.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOkAndHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task CreateAndRedirect_EndToEndWorkflow_Succeeds()
    {
        // 1. Create short URL
        var createRequest = new CreateUrlRequest(
            TargetUrl: "https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview",
            CustomAlias: "dotnet-10-overview",
            TtlSeconds: 3600
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/urls", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUrl = await createResponse.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(createdUrl);
        Assert.Equal("dotnet-10-overview", createdUrl.ShortCode);

        // 2. Redirect to Target
        var redirectResponse = await _client.GetAsync($"/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(createRequest.TargetUrl, redirectResponse.Headers.Location?.ToString());

        // 3. Check URL stats (visit count should be 1)
        var statsResponse = await _client.GetAsync($"/api/v1/urls/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var stats = await statsResponse.Content.ReadFromJsonAsync<UrlStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(1, stats.VisitCount);
        Assert.False(stats.IsExpired);

        // 4. Check Global Stats
        var globalStatsResponse = await _client.GetAsync("/api/v1/stats");
        Assert.Equal(HttpStatusCode.OK, globalStatsResponse.StatusCode);
        var globalStats = await globalStatsResponse.Content.ReadFromJsonAsync<GlobalStatsResponse>();
        Assert.NotNull(globalStats);
        Assert.True(globalStats.TotalUrls >= 1);
        Assert.True(globalStats.TotalVisits >= 1);

        // 5. Delete URL
        var deleteResponse = await _client.DeleteAsync($"/api/v1/urls/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 6. After deletion, redirect should return 404
        var redirectAfterDelete = await _client.GetAsync($"/{createdUrl.ShortCode}");
        Assert.Equal(HttpStatusCode.NotFound, redirectAfterDelete.StatusCode);
    }

    [Fact]
    public async Task CreateUrl_InvalidRequest_ReturnsBadRequest()
    {
        var invalidRequest = new CreateUrlRequest("not-a-valid-url");
        var response = await _client.PostAsJsonAsync("/api/v1/urls", invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonExistentUrl_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/non-existent-code-12345");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResponseHeaders_ContainRateLimitInfo()
    {
        var response = await _client.GetAsync("/api/v1/stats");
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
    }
}
