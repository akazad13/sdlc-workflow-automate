using NanoLink.Orchestrator.Core.Stages;
using Spectre.Console;

namespace NanoLink.Orchestrator.Core;

public class SdlcPipeline
{
    private readonly List<ISdlcStage> _stages =
    [
        new Stage1Triage(),
        new Stage2Plan(),
        new Stage3Code(),
        new Stage4CiSelfHealing(),
        new Stage5QualityGate(),
        new Stage6Release()
    ];

    public async Task<bool> RunAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.Write(
            new FigletText("NanoLink SDLC")
                .Color(Color.Cyan1));

        AnsiConsole.Write(new Rule("[yellow]Autonomous SDLC Lifecycle Automation Engine (.NET 10)[/]")
            .RuleStyle("grey")
            .LeftJustified());

        AnsiConsole.WriteLine();

        var stageResults = new List<(ISdlcStage Stage, bool Success, TimeSpan Duration)>();

        foreach (var stage in _stages)
        {
            var panel = new Panel(new Markup($"[bold white]Phase {stage.StageNumber}: {stage.Name}[/]\n[grey]{stage.Description}[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1);
            AnsiConsole.Write(panel);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool success = false;

            try
            {
                success = await stage.ExecuteAsync(context, ct);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                success = false;
            }

            stopwatch.Stop();
            stageResults.Add((stage, success, stopwatch.Elapsed));

            AnsiConsole.WriteLine();

            if (!success)
            {
                AnsiConsole.MarkupLine("[bold red]⛔ Pipeline halted due to failure in Phase {0}: {1}[/]", stage.StageNumber, stage.Name);
                return false;
            }
        }

        // Render Final Pipeline Execution Summary
        RenderSummary(context, stageResults);
        return true;
    }

    private static void RenderSummary(SdlcContext context, List<(ISdlcStage Stage, bool Success, TimeSpan Duration)> stageResults)
    {
        var summaryTable = new Table()
            .Border(TableBorder.HeavyHead)
            .Title("[bold green]✔ Autonomous SDLC Lifecycle Execution Succeeded[/]");

        summaryTable.AddColumn("[bold]Phase[/]");
        summaryTable.AddColumn("[bold]Stage Name[/]");
        summaryTable.AddColumn("[bold]Status[/]");
        summaryTable.AddColumn("[bold]Duration[/]");

        foreach (var (stage, success, duration) in stageResults)
        {
            summaryTable.AddRow(
                $"Phase {stage.StageNumber}",
                stage.Name,
                success ? "[green]✔ PASSED[/]" : "[red]✖ FAILED[/]",
                $"{duration.TotalSeconds:F2}s"
            );
        }

        AnsiConsole.Write(summaryTable);

        var metricsTable = new Table().Border(TableBorder.Rounded);
        metricsTable.AddColumn("[bold]Metric[/]");
        metricsTable.AddColumn("[bold]Value[/]");

        metricsTable.AddRow("Target Issue", $"#{context.IssueNumber} ({context.IssueTitle})");
        metricsTable.AddRow("Feature Branch", $"[yellow]{context.BranchName}[/]");
        metricsTable.AddRow("xUnit CI Test Suite", $"[green]{context.PassedTests} Passed[/], {context.FailedTests} Failed");
        metricsTable.AddRow("Roslyn Quality Score", $"[bold green]{context.QualityScore:F1}%[/]");
        metricsTable.AddRow("Released Version", $"[cyan]v{context.ReleasedVersion}[/]");
        metricsTable.AddRow("Artifacts Directory", $"[blue]{context.ArtifactsDirectory}[/]");

        AnsiConsole.Write(new Panel(metricsTable).Header("[bold]Pipeline Metrics & Artifacts[/]").BorderColor(Color.Green));
    }
}
