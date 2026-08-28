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
}
