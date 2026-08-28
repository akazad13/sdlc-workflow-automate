using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NanoLink.Orchestrator.QualityGates;

public record TestExecutionResult(
    bool Success,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    string Output
);

public static class DotnetTestRunner
{
    public static async Task<TestExecutionResult> RunTestsAsync(string solutionDir, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test --verbosity normal --no-build",
            WorkingDirectory = solutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                return new TestExecutionResult(false, 0, 0, 1, "Failed to start dotnet test process.");
            }

            string stdout = await process.StandardOutput.ReadToEndAsync(ct);
            string stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            string fullOutput = stdout + "\n" + stderr;

            int total = 0;
            int passed = 0;
            int failed = 0;

            var totalMatch = Regex.Match(stdout, @"Total tests:\s*(\d+)");
            if (totalMatch.Success) int.TryParse(totalMatch.Groups[1].Value, out total);

            var passedMatch = Regex.Match(stdout, @"Passed:\s*(\d+)");
            if (passedMatch.Success) int.TryParse(passedMatch.Groups[1].Value, out passed);

            var failedMatch = Regex.Match(stdout, @"Failed:\s*(\d+)");
            if (failedMatch.Success) int.TryParse(failedMatch.Groups[1].Value, out failed);

            // If regex didn't find summary, count passed lines
            if (total == 0)
            {
                var matches = Regex.Matches(stdout, @"Passed\s+[\w\.]+");
                passed = matches.Count;
                total = passed;
            }

            bool success = process.ExitCode == 0 && failed == 0;
            return new TestExecutionResult(success, total, passed, failed, fullOutput);
        }
        catch (Exception ex)
        {
            return new TestExecutionResult(false, 0, 0, 1, ex.Message);
        }
    }
}
