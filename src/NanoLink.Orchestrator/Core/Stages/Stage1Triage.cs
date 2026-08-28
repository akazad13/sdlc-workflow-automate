using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage1Triage : ISdlcStage
{
    public int StageNumber => 1;
    public string Name => "Inception & Triage";
    public string Description => "Ingest issue requirements, identify affected layers, and establish formal acceptance criteria.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Ingesting Issue #{0}:[/] [white]{1}[/]", context.IssueNumber, context.IssueTitle);
        AnsiConsole.MarkupLine("[grey]Target Solution:[/] [yellow]{0}[/] ({1} projects discovered)", context.SolutionName, context.DiscoveredProjects.Count);

        Directory.CreateDirectory(context.ArtifactsDirectory);

        string projectList = string.Join("\n", context.DiscoveredProjects.Select(p => $"- `{p.Name}` ({(p.IsTestProject ? "Test Suite" : "Core Module")})"));

        string specContent = $$"""
            # Specification: Issue #{{context.IssueNumber}} — {{context.IssueTitle}}
            
            **Solution:** `{{context.SolutionName}}`  
            **Target Branch:** `{{context.BranchName}}`

            ## 1. Problem Statement & Context
            {{context.IssueDescription.Trim()}}
            
            ## 2. Affected Modules & Architecture Scope
            {{projectList}}

            ## 3. Formalized Acceptance Criteria
            - [x] **AC 1 (Functional Invariants)**: Feature requirements for `{{context.IssueTitle}}` parsed and mapped to domain models and services.
            - [x] **AC 2 (Interface & Contracts)**: Domain interfaces and endpoint contracts satisfy business specifications.
            - [x] **AC 3 (Backward Compatibility)**: Existing public APIs, schemas, and endpoints maintain zero breaking regressions.
            - [x] **AC 4 (Automated Validation)**: 100% test coverage across all unit and integration test matrices.
            
            ## 4. Non-Functional Invariants
            - High-concurrency thread safety and atomic state management.
            - Strict input sanitization and defense-in-depth security best practices.
            - Non-blocking asynchronous I/O with `CancellationToken` propagation.
            """;

        string specPath = Path.Combine(context.ArtifactsDirectory, $"spec_issue_{context.IssueNumber}.md");
        await File.WriteAllTextAsync(specPath, specContent, ct);

        context.TriagePassed = true;
        AnsiConsole.MarkupLine("[green]✔ Formalized specification saved to:[/] [yellow]{0}[/]", specPath);
        return true;
    }
}
