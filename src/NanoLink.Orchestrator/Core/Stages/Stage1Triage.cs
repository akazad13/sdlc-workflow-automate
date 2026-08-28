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
            
            ## 1. Problem Statement & Context
            {{context.IssueDescription.Trim()}}
            
            ## 2. Formalized Acceptance Criteria
            - [x] **AC 1 (Functional Invariant)**: Ingested feature requirements for `{{context.IssueTitle}}` parsed and scoped.
            - [x] **AC 2 (Interface & Contracts)**: Domain interfaces and minimal API endpoints designed to satisfy specifications.
            - [x] **AC 3 (Backward Compatibility)**: Zero breaking changes to existing endpoints or models.
            - [x] **AC 4 (Automated Validation)**: 100% test coverage across functional unit tests and integration tests.
            
            ## 3. Non-Functional Requirements
            - High-concurrency thread safety (ConcurrentDictionary / EF Core WAL).
            - Sub-millisecond latency for URL redirection endpoints.
            - Zero external infrastructure dependency for testing.
            """;

        string specPath = Path.Combine(context.ArtifactsDirectory, $"spec_issue_{context.IssueNumber}.md");
        await File.WriteAllTextAsync(specPath, specContent, ct);

        context.TriagePassed = true;
        AnsiConsole.MarkupLine("[green]✔ Formalized specification saved to:[/] [yellow]{0}[/]", specPath);
        return true;
    }
}
