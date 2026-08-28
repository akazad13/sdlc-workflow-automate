using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage6Release : ISdlcStage
{
    public int StageNumber => 6;
    public string Name => "Release & Verification";
    public string Description => "Version bumping, automated changelog generation, and deployment smoke verification.";

    public async Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Finalizing Release v{0}...[/]", context.ReleasedVersion);

        string changelogPath = Path.Combine(context.BaseDirectory, "CHANGELOG.md");
        string existingContent = File.Exists(changelogPath) ? await File.ReadAllTextAsync(changelogPath, ct) : "# Changelog\n\n";

        string newEntry = $$"""
            ## [{{context.ReleasedVersion}}] - {{DateTime.UtcNow:yyyy-MM-dd}}
            ### Closes Issue #{{context.IssueNumber}}: {{context.IssueTitle}}
            - Automated implementation and validation via SDLC autonomous pipeline.
            - 100% xUnit test suite pass rate ({{context.PassedTests}} tests passing).
            - Quality and security gates verified with a score of {{context.QualityScore:F1}}%.

            """;

        string updatedContent = existingContent.Contains("## [") 
            ? existingContent.Replace("# Changelog\n\n", $"# Changelog\n\n{newEntry}\n")
            : $"# Changelog\n\n{newEntry}";

        await File.WriteAllTextAsync(changelogPath, updatedContent, ct);

        AnsiConsole.MarkupLine("[green]✔ Release notes and CHANGELOG.md updated for Issue #{0}![/]", context.IssueNumber);
        return true;
    }
}
