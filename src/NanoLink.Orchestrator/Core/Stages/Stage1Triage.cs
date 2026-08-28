using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage1Triage : ISdlcStage
{
    public int StageNumber => 1;
    public string Name => "Inception & Triage";
    public string Description => "Ingest GitHub issue, formalize requirements, and establish acceptance criteria.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Ingesting GitHub Issue #{0}:[/] [white]{1}[/]", context.IssueNumber, context.IssueTitle);

        Directory.CreateDirectory(context.ArtifactsDirectory);

        string specContent = $$"""
            # Specification: Issue #{{context.IssueNumber}} — {{context.IssueTitle}}
            
            ## 1. Problem Statement
            NanoLink currently provides basic URL redirection. High-traffic production requirements necessitate:
            - Preventing database / memory clutter via Time-To-Live (TTL) URL expiration and background eviction.
            - Preventing denial-of-service (DoS) attacks via IP-based Token-Bucket Rate Limiting (10 req/min).
            - Exposing real-time analytics for active vs expired URLs and total visits via `/api/v1/stats`.
            
            ## 2. Acceptance Criteria
            1. **TTL Expiration**:
               - `POST /api/v1/urls` accepts optional `ttlSeconds` (integer > 0).
               - Expired URLs return HTTP 404 on redirect attempt `GET /{shortCode}`.
               - Expired URLs are excluded from active counts and purged by `UrlCleanupBackgroundService`.
            2. **Token-Bucket Rate Limiting**:
               - Enforce 10 req/min limit per client IP with burst capacity of 10.
               - Exhausted limit returns HTTP 429 Too Many Requests with `Retry-After` header.
               - Every response includes `X-RateLimit-Limit` and `X-RateLimit-Remaining` headers.
            3. **Analytics API**:
               - `GET /api/v1/stats` returns `totalUrls`, `activeUrls`, `expiredUrls`, and `totalVisits`.
               - `GET /api/v1/urls/{shortCode}` returns detailed stats per short URL (visit count, last accessed timestamp).
            
            ## 3. Non-Functional Requirements
            - High-concurrency thread safety (ConcurrentDictionary + atomic updates).
            - Sub-millisecond latency for redirection endpoints.
            - Zero external infrastructure dependency for testing (In-Memory + WebApplicationFactory).
            """;

        string specPath = Path.Combine(context.ArtifactsDirectory, $"spec_issue_{context.IssueNumber}.md");
        await File.WriteAllTextAsync(specPath, specContent, ct);

        context.TriagePassed = true;
        AnsiConsole.MarkupLine("[green]✔ Formalized specification saved to:[/] [yellow]{0}[/]", specPath);
        return true;
    }
}
