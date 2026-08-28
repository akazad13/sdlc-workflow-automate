using System.Text.Json;
using System.Text.RegularExpressions;

namespace NanoLink.Orchestrator.Core;

public record DiscoveredProject(
    string Name,
    string ProjectPath,
    string RelativePath,
    bool IsTestProject
);

public class SdlcContext
{
    public int IssueNumber { get; set; } = 1;
    public string IssueTitle { get; set; } = "Autonomous Feature Implementation";
    public string IssueDescription { get; set; } = "Generic SDLC requirement";

    public string SolutionName { get; set; } = "Solution";
    public string SolutionFilePath { get; set; } = string.Empty;
    public string BranchName { get; set; } = "feat/autonomous-impl";
    public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string ArtifactsDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");

    public List<DiscoveredProject> DiscoveredProjects { get; set; } = [];

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
        var context = new SdlcContext();

        // 1. Resolve Solution Root by scanning for .sln, .slnx, or .git
        context.BaseDirectory = FindSolutionRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
        context.ArtifactsDirectory = Path.Combine(context.BaseDirectory, "artifacts");

        // 2. Discover Solution Name and Projects
        DiscoverProjects(context);

        // 3. Check if running inside GitHub Actions with GITHUB_EVENT_PATH
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
                        context.IssueNumber = numProp.GetInt32();

                    if (issueProp.TryGetProperty("title", out var titleProp))
                        context.IssueTitle = titleProp.GetString() ?? context.IssueTitle;

                    if (issueProp.TryGetProperty("body", out var bodyProp))
                        context.IssueDescription = bodyProp.GetString() ?? context.IssueDescription;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to parse GITHUB_EVENT_PATH: {ex.Message}");
            }
        }

        // 4. Check Environment Variables (ISSUE_NUMBER, ISSUE_TITLE, ISSUE_BODY)
        string? envNum = Environment.GetEnvironmentVariable("ISSUE_NUMBER");
        if (int.TryParse(envNum, out int parsedNum)) context.IssueNumber = parsedNum;

        string? envTitle = Environment.GetEnvironmentVariable("ISSUE_TITLE");
        if (!string.IsNullOrWhiteSpace(envTitle)) context.IssueTitle = envTitle;

        string? envBody = Environment.GetEnvironmentVariable("ISSUE_BODY");
        if (!string.IsNullOrWhiteSpace(envBody)) context.IssueDescription = envBody;

        // 5. Check CLI Arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--issue" && i + 1 < args.Length && int.TryParse(args[i + 1], out int cliNum))
                context.IssueNumber = cliNum;
            else if (args[i] == "--title" && i + 1 < args.Length)
                context.IssueTitle = args[i + 1];
            else if (args[i] == "--desc" && i + 1 < args.Length)
                context.IssueDescription = args[i + 1];
            else if (args[i] == "--dir" && i + 1 < args.Length)
                context.BaseDirectory = Path.GetFullPath(args[i + 1]);
        }

        // Generate dynamic branch name & version based on issue
        string slug = Slugify(context.IssueTitle);
        context.BranchName = $"feat/issue-{context.IssueNumber}-{slug}";
        context.ReleasedVersion = $"1.{Math.Max(1, context.IssueNumber % 100)}.0";

        return context;
    }

    private static void DiscoverProjects(SdlcContext context)
    {
        var slnFiles = Directory.GetFiles(context.BaseDirectory, "*.sln*", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length > 0)
        {
            context.SolutionFilePath = slnFiles[0];
            context.SolutionName = Path.GetFileNameWithoutExtension(slnFiles[0]);
        }
        else
        {
            context.SolutionName = new DirectoryInfo(context.BaseDirectory).Name;
        }

        var csprojFiles = Directory.GetFiles(context.BaseDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin"));

        foreach (var proj in csprojFiles)
        {
            string name = Path.GetFileNameWithoutExtension(proj);
            string rel = Path.GetRelativePath(context.BaseDirectory, proj);
            bool isTest = name.Contains("Test", StringComparison.OrdinalIgnoreCase) || rel.Contains("test", StringComparison.OrdinalIgnoreCase);

            context.DiscoveredProjects.Add(new DiscoveredProject(name, proj, rel, isTest));
        }
    }

    private static string? FindSolutionRoot(string startDir)
    {
        var current = new DirectoryInfo(startDir);
        while (current != null)
        {
            if (current.GetFiles("*.sln*").Length > 0 || current.GetDirectories(".git").Length > 0)
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
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
