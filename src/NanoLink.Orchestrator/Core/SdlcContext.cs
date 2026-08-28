using System.Text.Json;
using System.Text.RegularExpressions;

namespace NanoLink.Orchestrator.Core;

public class SdlcContext
{
    public int IssueNumber { get; set; } = 101;
    public string IssueTitle { get; set; } = "Implement TTL URL Expiration, Rate Limiting Middleware, and Real-Time Analytics";
    public string IssueDescription { get; set; } = """
        Feature Request:
        1. Add TTL-based URL expiration with background cleanup worker.
        2. Implement Token-Bucket Rate Limiting (10 req/min per IP) with 429 Retry-After headers.
        3. Provide real-time analytics /api/v1/stats reporting active vs expired URLs and total visits.
        """;

    public string BranchName { get; set; } = "feat/issue-101-ttl-ratelimit-stats";
    public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string ArtifactsDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");

    public bool TriagePassed { get; set; }
    public bool PlanningPassed { get; set; }
    public bool CodingPassed { get; set; }
    public bool CiPassed { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int RepairAttempts { get; set; }
    public double QualityScore { get; set; }
    public string CurrentVersion { get; set; } = "1.0.0";
    public string ReleasedVersion { get; set; } = "1.1.0";
    public bool IsGitHubActions { get; set; }

    public static SdlcContext InitializeFromEnvironment(string[] args)
    {
        var context = new SdlcContext
        {
            BaseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."))
        };

        if (!File.Exists(Path.Combine(context.BaseDirectory, "NanoLink.slnx")) &&
            !File.Exists(Path.Combine(context.BaseDirectory, "NanoLink.sln")))
        {
            context.BaseDirectory = Directory.GetCurrentDirectory();
        }

        context.ArtifactsDirectory = Path.Combine(context.BaseDirectory, "artifacts");

        // 1. Check if running inside GitHub Actions with GITHUB_EVENT_PATH
        string? eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
        if (!string.IsNullOrWhiteSpace(eventPath) && File.Exists(eventPath))
        {
            context.IsGitHubActions = true;
            try
            {
                string json = File.ReadAllText(eventPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("issue", out var issueProp))
                {
                    if (issueProp.TryGetProperty("number", out var numProp))
                    {
                        context.IssueNumber = numProp.GetInt32();
                    }

                    if (issueProp.TryGetProperty("title", out var titleProp))
                    {
                        context.IssueTitle = titleProp.GetString() ?? context.IssueTitle;
                    }

                    if (issueProp.TryGetProperty("body", out var bodyProp))
                    {
                        context.IssueDescription = bodyProp.GetString() ?? context.IssueDescription;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to parse GITHUB_EVENT_PATH: {ex.Message}");
            }
        }

        // 2. Check Environment Variables (ISSUE_NUMBER, ISSUE_TITLE, ISSUE_BODY)
        string? envNum = Environment.GetEnvironmentVariable("ISSUE_NUMBER");
        if (int.TryParse(envNum, out int parsedNum)) context.IssueNumber = parsedNum;

        string? envTitle = Environment.GetEnvironmentVariable("ISSUE_TITLE");
        if (!string.IsNullOrWhiteSpace(envTitle)) context.IssueTitle = envTitle;

        string? envBody = Environment.GetEnvironmentVariable("ISSUE_BODY");
        if (!string.IsNullOrWhiteSpace(envBody)) context.IssueDescription = envBody;

        // 3. Check CLI Arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--issue" && i + 1 < args.Length && int.TryParse(args[i + 1], out int cliNum))
            {
                context.IssueNumber = cliNum;
            }
            else if (args[i] == "--title" && i + 1 < args.Length)
            {
                context.IssueTitle = args[i + 1];
            }
            else if (args[i] == "--desc" && i + 1 < args.Length)
            {
                context.IssueDescription = args[i + 1];
            }
        }

        // Generate dynamic branch name & version based on issue number and title
        string slug = Slugify(context.IssueTitle);
        context.BranchName = $"feat/issue-{context.IssueNumber}-{slug}";
        context.ReleasedVersion = $"1.{context.IssueNumber % 100}.0";

        return context;
    }

    private static string Slugify(string text)
    {
        string slug = text.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        if (slug.Length > 30) slug = slug[..30].TrimEnd('-');
        return string.IsNullOrWhiteSpace(slug) ? "feature" : slug;
    }
}
