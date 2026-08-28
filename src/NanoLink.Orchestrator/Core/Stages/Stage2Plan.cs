using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage2Plan : ISdlcStage
{
    public int StageNumber => 2;
    public string Name => "Architecture & Planning";
    public string Description => "Generate technical implementation plan, component decomposition, and test matrices.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Formulating Architecture & C# Design Plan for Issue #{0}...[/]", context.IssueNumber);

        var coreProjects = context.DiscoveredProjects.Where(p => !p.IsTestProject).ToList();
        var testProjects = context.DiscoveredProjects.Where(p => p.IsTestProject).ToList();

        string componentDecomp = string.Join("\n", coreProjects.Select(p => 
            $"- **`{p.Name}`** (`{p.RelativePath}`):\n  - Target implementations, contracts, domain models, and service registrations."));

        string testDecomp = string.Join("\n", testProjects.Select(p => 
            $"| `{p.Name}` | Automated Test Suite | Unit & integration assertions for `{context.IssueTitle}` |"));

        string planContent = $$"""
            # Implementation Plan: Issue #{{context.IssueNumber}} — {{context.IssueTitle}}

            **Solution Architecture:** `{{context.SolutionName}}`

            ## 1. Component Architecture & Decomposition
            {{componentDecomp}}

            ## 2. Test Verification Matrix
            | Test Project | Scope | Target Invariants |
            | :--- | :--- | :--- |
            {{testDecomp}}

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
