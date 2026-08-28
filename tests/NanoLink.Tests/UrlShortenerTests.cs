using NanoLink.Api.Models;
using NanoLink.Api.Services;
using NanoLink.Api.Storage;
using Xunit;

namespace NanoLink.Tests;

public class UrlShortenerTests
{
    private readonly IUrlRepository _repository;
    private readonly UrlShortenerService _service;
    private const string BaseUrl = "https://nano.link";

    public UrlShortenerTests()
    {
        _repository = new InMemoryUrlRepository();
        _service = new UrlShortenerService(_repository);
    }

    [Fact]
    public async Task ShortenUrl_ValidUrl_GeneratesShortCodeSuccessfully()
    {
        // Arrange
        var request = new CreateUrlRequest("https://github.com/dotnet/aspnetcore");

        // Act
        var (success, error, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ShortCode));
        Assert.Equal($"{BaseUrl}/{result.ShortCode}", result.ShortUrl);
        Assert.Equal("https://github.com/dotnet/aspnetcore", result.TargetUrl);
        Assert.Null(result.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ShortenUrl_EmptyUrl_ReturnsError(string? invalidUrl)
    {
        // Arrange
        var request = new CreateUrlRequest(invalidUrl!);

        // Act
        var (success, error, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.False(success);
        Assert.Equal("Target URL cannot be empty.", error);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://invalid-scheme.com/file")]
    [InlineData("javascript:alert(1)")]
    public async Task ShortenUrl_InvalidScheme_ReturnsError(string invalidUrl)
    {
        // Arrange
        var request = new CreateUrlRequest(invalidUrl);

        // Act
        var (success, error, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.False(success);
        Assert.Contains("Only HTTP and HTTPS schemes are supported", error);
        Assert.Null(result);
    }

    [Fact]
    public async Task ShortenUrl_WithCustomAlias_UsesCustomAlias()
    {
        // Arrange
        var customAlias = "custom-docs-2026";
        var request = new CreateUrlRequest("https://learn.microsoft.com/dotnet", CustomAlias: customAlias);

        // Act
        var (success, error, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(customAlias, result.ShortCode);
        Assert.Equal($"{BaseUrl}/{customAlias}", result.ShortUrl);
    }

    [Theory]
    [InlineData("ab")] // too short (<3)
    [InlineData("invalid alias with spaces")]
    [InlineData("alias$with@special#chars")]
    public async Task ShortenUrl_InvalidCustomAliasFormat_ReturnsError(string invalidAlias)
    {
        // Arrange
        var request = new CreateUrlRequest("https://google.com", CustomAlias: invalidAlias);

        // Act
        var (success, error, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.False(success);
        Assert.Contains("Custom alias must be 3-32 characters long", error);
    }

    [Fact]
    public async Task ShortenUrl_DuplicateCustomAlias_ReturnsConflictError()
    {
        // Arrange
        var alias = "my-awesome-link";
        var request1 = new CreateUrlRequest("https://first.com", CustomAlias: alias);
        var request2 = new CreateUrlRequest("https://second.com", CustomAlias: alias);

        // Act
        var (success1, _, _) = await _service.ShortenUrlAsync(request1, BaseUrl);
        var (success2, error2, result2) = await _service.ShortenUrlAsync(request2, BaseUrl);

        // Assert
        Assert.True(success1);
        Assert.False(success2);
        Assert.Contains("already in use", error2);
        Assert.Null(result2);
    }

    [Fact]
    public async Task ShortenUrl_WithTtl_SetsCorrectExpiration()
    {
        // Arrange
        int ttlSeconds = 3600;
        var request = new CreateUrlRequest("https://example.com", TtlSeconds: ttlSeconds);

        // Act
        var (success, _, result) = await _service.ShortenUrlAsync(request, BaseUrl);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.NotNull(result.ExpiresAtUtc);
        var expectedMinExpiry = DateTime.UtcNow.AddSeconds(ttlSeconds - 2);
        var expectedMaxExpiry = DateTime.UtcNow.AddSeconds(ttlSeconds + 2);
        Assert.InRange(result.ExpiresAtUtc.Value, expectedMinExpiry, expectedMaxExpiry);
    }

    [Fact]
    public async Task GetTarget_RecordsVisitCountAndLastAccessed()
    {
        // Arrange
        var request = new CreateUrlRequest("https://example.com/target");
        var (_, _, createResult) = await _service.ShortenUrlAsync(request, BaseUrl);
        var shortCode = createResult!.ShortCode;

        // Act
        var retrieved1 = await _service.GetTargetAsync(shortCode);
        var retrieved2 = await _service.GetTargetAsync(shortCode);
        var stats = await _service.GetStatsAsync(shortCode);

        // Assert
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.NotNull(stats);
        Assert.Equal(2, stats.VisitCount);
        Assert.NotNull(stats.LastAccessedUtc);
    }

    [Fact]
    public async Task DeleteUrl_RemovesUrlSuccessfully()
    {
        // Arrange
        var request = new CreateUrlRequest("https://example.com/to-delete");
        var (_, _, createResult) = await _service.ShortenUrlAsync(request, BaseUrl);
        var shortCode = createResult!.ShortCode;

        // Act
        var deleteResult = await _service.DeleteUrlAsync(shortCode);
        var retrieved = await _service.GetTargetAsync(shortCode);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(retrieved);
    }
}
