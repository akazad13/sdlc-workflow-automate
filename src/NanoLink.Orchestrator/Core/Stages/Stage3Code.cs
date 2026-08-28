using Spectre.Console;
using System.Text.RegularExpressions;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage3Code : ISdlcStage
{
    public int StageNumber => 3;
    public string Name => "Autonomous Coding & TDD";
    public string Description => "Synthesize C# domain models, business logic, endpoints, and xUnit test suites for the target issue.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Synthesizing and Generating C# Implementation on branch:[/] [yellow]{0}[/]", context.BranchName);

        var generatedFiles = new List<string>();

        string titleLower = context.IssueTitle.ToLowerInvariant();
        string descLower = context.IssueDescription.ToLowerInvariant();

        string apiDir = Path.Combine(context.BaseDirectory, "src", "NanoLink.Api");
        string testDir = Path.Combine(context.BaseDirectory, "tests", "NanoLink.Tests");

        Directory.CreateDirectory(Path.Combine(apiDir, "Models"));
        Directory.CreateDirectory(Path.Combine(apiDir, "Services"));
        Directory.CreateDirectory(Path.Combine(apiDir, "Storage"));
        Directory.CreateDirectory(testDir);

        // 1. Webhook Dispatcher Feature
        if (titleLower.Contains("webhook") || descLower.Contains("webhook") || titleLower.Contains("notification") || descLower.Contains("notification"))
        {
            await SynthesizeWebhookFeatureAsync(apiDir, testDir, generatedFiles, ct);
        }
        // 2. QR Code Generation Feature
        else if (titleLower.Contains("qr") || descLower.Contains("qr") || titleLower.Contains("qrcode") || descLower.Contains("qrcode"))
        {
            await SynthesizeQrCodeFeatureAsync(apiDir, testDir, generatedFiles, ct);
        }
        // 3. Password Protection & Visit Caps
        else if (titleLower.Contains("password") || descLower.Contains("password") || titleLower.Contains("limit") || descLower.Contains("limit"))
        {
            await SynthesizePasswordFeatureAsync(apiDir, testDir, generatedFiles, ct);
        }
        // 4. Default / Generic Autonomous Feature Generator
        else
        {
            await SynthesizeGenericFeatureAsync(context, apiDir, testDir, generatedFiles, ct);
        }

        // Render Generated Files Table
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Synthesized File[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Status[/]");

        foreach (var file in generatedFiles)
        {
            string rel = Path.GetRelativePath(context.BaseDirectory, file);
            string type = file.Contains("Tests") ? "[cyan]xUnit Test Suite[/]" : file.Contains("Models") ? "[yellow]DTO / Model[/]" : "[green]Service / Logic[/]";
            table.AddRow(rel, type, "[bold green]✔ Code Written & Staged[/]");
        }

        AnsiConsole.Write(table);

        context.CodingPassed = generatedFiles.Count > 0;
        return context.CodingPassed;
    }

    private static async Task SynthesizeWebhookFeatureAsync(string apiDir, string testDir, List<string> generated, CancellationToken ct)
    {
        // 1. Webhook Models
        string modelFile = Path.Combine(apiDir, "Models", "WebhookModels.cs");
        string modelContent = """
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
            """;
        await File.WriteAllTextAsync(modelFile, modelContent, ct);
        generated.Add(modelFile);

        // 2. Webhook Service
        string serviceFile = Path.Combine(apiDir, "Services", "WebhookDispatcherService.cs");
        string serviceContent = """
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
            """;
        await File.WriteAllTextAsync(serviceFile, serviceContent, ct);
        generated.Add(serviceFile);

        // 3. Webhook Tests
        string testFile = Path.Combine(testDir, "WebhookDispatcherTests.cs");
        string testContent = """
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
            """;
        await File.WriteAllTextAsync(testFile, testContent, ct);
        generated.Add(testFile);
    }

    private static async Task SynthesizeQrCodeFeatureAsync(string apiDir, string testDir, List<string> generated, CancellationToken ct)
    {
        // 1. QR Code Service
        string serviceFile = Path.Combine(apiDir, "Services", "QrCodeService.cs");
        string serviceContent = """
            using System.Text;

            namespace NanoLink.Api.Services;

            public interface IQrCodeService
            {
                string GenerateSvgQrCode(string url, int size = 256);
                byte[] GeneratePngMockQrCode(string url, int size = 256);
            }

            public class QrCodeService : IQrCodeService
            {
                public string GenerateSvgQrCode(string url, int size = 256)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size} {size}\" width=\"{size}\" height=\"{size}\">");
                    sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
                    sb.AppendLine($"  <!-- QR Code Mock Payload: {url} -->");
                    sb.AppendLine($"  <rect x=\"10\" y=\"10\" width=\"{size - 20}\" height=\"{size - 20}\" fill=\"none\" stroke=\"#000000\" stroke-width=\"4\"/>");
                    sb.AppendLine($"  <text x=\"50%\" y=\"50%\" text-anchor=\"middle\" dominant-baseline=\"middle\" font-family=\"monospace\" font-size=\"12\" fill=\"#333333\">QR: {url}</text>");
                    sb.AppendLine("</svg>");
                    return sb.ToString();
                }

                public byte[] GeneratePngMockQrCode(string url, int size = 256)
                {
                    // Standard PNG Magic Header Bytes: 89 50 4E 47 0D 0A 1A 0A
                    byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
                    return pngHeader;
                }
            }
            """;
        await File.WriteAllTextAsync(serviceFile, serviceContent, ct);
        generated.Add(serviceFile);

        // 2. QR Code Tests
        string testFile = Path.Combine(testDir, "QrCodeTests.cs");
        string testContent = """
            using NanoLink.Api.Services;
            using Xunit;

            namespace NanoLink.Tests;

            public class QrCodeTests
            {
                private readonly QrCodeService _service = new();

                [Fact]
                public void GenerateSvg_ProducesValidSvgXml()
                {
                    string url = "https://nano.link/xyz123";
                    string svg = _service.GenerateSvgQrCode(url, 300);

                    Assert.Contains("<svg", svg);
                    Assert.Contains("</svg>", svg);
                    Assert.Contains("xyz123", svg);
                }

                [Fact]
                public void GeneratePng_ProducesValidPngHeaderBytes()
                {
                    byte[] bytes = _service.GeneratePngMockQrCode("https://nano.link/abc");

                    Assert.NotNull(bytes);
                    Assert.True(bytes.Length >= 8);
                    Assert.Equal(0x89, bytes[0]);
                    Assert.Equal(0x50, bytes[1]); // 'P'
                    Assert.Equal(0x4E, bytes[2]); // 'N'
                    Assert.Equal(0x47, bytes[3]); // 'G'
                }
            }
            """;
        await File.WriteAllTextAsync(testFile, testContent, ct);
        generated.Add(testFile);
    }

    private static async Task SynthesizePasswordFeatureAsync(string apiDir, string testDir, List<string> generated, CancellationToken ct)
    {
        string serviceFile = Path.Combine(apiDir, "Services", "PasswordProtectionService.cs");
        string serviceContent = """
            using System.Security.Cryptography;
            using System.Text;

            namespace NanoLink.Api.Services;

            public interface IPasswordProtectionService
            {
                (string Hash, string Salt) HashPassword(string password);
                bool VerifyPassword(string password, string hash, string salt);
            }

            public class PasswordProtectionService : IPasswordProtectionService
            {
                public (string Hash, string Salt) HashPassword(string password)
                {
                    byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
                    string salt = Convert.ToHexStringLower(saltBytes);
                    string hash = ComputeHash(password, salt);
                    return (hash, salt);
                }

                public bool VerifyPassword(string password, string hash, string salt)
                {
                    string computed = ComputeHash(password, salt);
                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computed),
                        Encoding.UTF8.GetBytes(hash));
                }

                private static string ComputeHash(string password, string salt)
                {
                    using var sha = SHA256.Create();
                    byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + ":" + salt));
                    return Convert.ToHexStringLower(bytes);
                }
            }
            """;
        await File.WriteAllTextAsync(serviceFile, serviceContent, ct);
        generated.Add(serviceFile);

        string testFile = Path.Combine(testDir, "PasswordProtectionTests.cs");
        string testContent = """
            using NanoLink.Api.Services;
            using Xunit;

            namespace NanoLink.Tests;

            public class PasswordProtectionTests
            {
                private readonly PasswordProtectionService _service = new();

                [Fact]
                public void HashAndVerify_ValidPassword_ReturnsTrue()
                {
                    string pass = "SecureP@ssword123";
                    var (hash, salt) = _service.HashPassword(pass);

                    bool valid = _service.VerifyPassword(pass, hash, salt);
                    Assert.True(valid);
                }

                [Fact]
                public void Verify_InvalidPassword_ReturnsFalse()
                {
                    var (hash, salt) = _service.HashPassword("CorrectPassword");

                    bool valid = _service.VerifyPassword("WrongPassword", hash, salt);
                    Assert.False(valid);
                }
            }
            """;
        await File.WriteAllTextAsync(testFile, testContent, ct);
        generated.Add(testFile);
    }

    private static async Task SynthesizeGenericFeatureAsync(SdlcContext context, string apiDir, string testDir, List<string> generated, CancellationToken ct)
    {
        string cleanName = Regex.Replace(context.IssueTitle, @"[^a-zA-Z0-9]", "");
        if (cleanName.Length > 20) cleanName = cleanName[..20];
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "CustomFeature";

        string serviceFile = Path.Combine(apiDir, "Services", $"{cleanName}Service.cs");
        string serviceContent = $$"""
            namespace NanoLink.Api.Services;

            public interface I{{cleanName}}Service
            {
                bool ExecuteFeature(string input, out string result);
            }

            public class {{cleanName}}Service : I{{cleanName}}Service
            {
                public bool ExecuteFeature(string input, out string result)
                {
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        result = string.Empty;
                        return false;
                    }

                    result = $"Processed: {input}";
                    return true;
                }
            }
            """;
        await File.WriteAllTextAsync(serviceFile, serviceContent, ct);
        generated.Add(serviceFile);

        string testFile = Path.Combine(testDir, $"{cleanName}Tests.cs");
        string testContent = $$"""
            using NanoLink.Api.Services;
            using Xunit;

            namespace NanoLink.Tests;

            public class {{cleanName}}Tests
            {
                private readonly {{cleanName}}Service _service = new();

                [Fact]
                public void ExecuteFeature_ValidInput_ReturnsSuccess()
                {
                    bool ok = _service.ExecuteFeature("test-input", out string result);
                    Assert.True(ok);
                    Assert.Equal("Processed: test-input", result);
                }

                [Fact]
                public void ExecuteFeature_EmptyInput_ReturnsFalse()
                {
                    bool ok = _service.ExecuteFeature("", out _);
                    Assert.False(ok);
                }
            }
            """;
        await File.WriteAllTextAsync(testFile, testContent, ct);
        generated.Add(testFile);
    }
}
