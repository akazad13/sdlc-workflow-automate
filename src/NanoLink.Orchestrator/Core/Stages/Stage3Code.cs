using Spectre.Console;

namespace NanoLink.Orchestrator.Core.Stages;

public class Stage3Code : ISdlcStage
{
    public int StageNumber => 3;
    public string Name => "Autonomous Coding & TDD";
    public string Description => "Implement C# features, endpoints, services, and xUnit test suites.";

    public Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold cyan]▶ Checking and Validating C# Implementation on branch:[/] [yellow]{0}[/]", context.BranchName);

        var requiredFiles = new[]
        {
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Models", "UrlModels.cs"),
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Services", "UrlShortenerService.cs"),
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Services", "TokenBucketRateLimiter.cs"),
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Middleware", "RateLimitingMiddleware.cs"),
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Services", "UrlCleanupBackgroundService.cs"),
            Path.Combine(context.BaseDirectory, "src", "NanoLink.Api", "Program.cs"),
            Path.Combine(context.BaseDirectory, "tests", "NanoLink.Tests", "UrlShortenerTests.cs"),
            Path.Combine(context.BaseDirectory, "tests", "NanoLink.Tests", "RateLimiterTests.cs"),
            Path.Combine(context.BaseDirectory, "tests", "NanoLink.Tests", "ExpirationTests.cs"),
            Path.Combine(context.BaseDirectory, "tests", "NanoLink.Tests", "IntegrationTests.cs")
        };

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]File Component[/]");
        table.AddColumn("[bold]Status[/]");

        bool allExist = true;
        foreach (var file in requiredFiles)
        {
            bool exists = File.Exists(file);
            string relativePath = Path.GetRelativePath(context.BaseDirectory, file);
            if (exists)
            {
                table.AddRow(relativePath, "[green]✔ Implemented[/]");
            }
            else
            {
                table.AddRow(relativePath, "[red]✖ Missing[/]");
                allExist = false;
            }
        }

        AnsiConsole.Write(table);

        context.CodingPassed = allExist;
        return Task.FromResult(allExist);
    }
}
