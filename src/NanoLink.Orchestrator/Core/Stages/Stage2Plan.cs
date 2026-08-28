using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage2Plan : ISdlcStage
{
    public int StageNumber => 2;
    public string Name => "Architecture & Planning";
    public string Description => "Generate technical implementation plan, component decomposition, and xUnit test matrix.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Formulating Architecture & C# Design Plan for Issue #{0}...[/]", context.IssueNumber);

        string planContent = $$"""
            # Implementation Plan: Issue #{{context.IssueNumber}} — {{context.IssueTitle}}

            ## 1. Component Architecture & Target Scopes
            - **Scope & Focus**: `{{context.IssueTitle}}`
            - **Target Projects**:
              - `src/NanoLink.Api`: ASP.NET Core Minimal APIs, Domain Models, Service Contracts, and Storage.
              - `src/NanoLink.Orchestrator`: CLI automation runner and quality gate evaluators.
              - `tests/NanoLink.Tests`: Comprehensive xUnit unit & integration testing matrices.

            ## 2. Test Verification Matrix
            | Test Suite | Scope | Target Invariants |
            | :--- | :--- | :--- |
            | `UrlShortenerTests` | Unit | Base62 generation, custom alias validation, collision retry, TTL calculation |
            | `RateLimiterTests` | Unit | Token acquisition, exhaustion, per-IP isolation, bucket reset |
            | `ExpirationTests` | Unit | Expired URL blocking, background cleanup eviction verification |
            | `IntegrationTests` | Integration | End-to-end WebApplicationFactory tests covering all HTTP status codes |

            ## 3. Git Branching Strategy
            - Target Branch: `main`
            - Working Branch: `{{context.BranchName}}`
            """;

        string planPath = Path.Combine(context.ArtifactsDirectory, $"plan_issue_{context.IssueNumber}.md");
        await File.WriteAllTextAsync(planPath, planContent, ct);

        context.PlanningPassed = true;
        AnsiConsole.MarkupLine("[green]✔ Architecture plan saved to:[/] [yellow]{0}[/]", planPath);
        return true;
    }
}
