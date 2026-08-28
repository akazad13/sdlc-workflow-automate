using NanoLink.Orchestrator.Core;

var context = new SdlcContext
{
    BaseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
};

// Fallback to current working directory if path doesn't contain solution
if (!File.Exists(Path.Combine(context.BaseDirectory, "NanoLink.slnx")) &&
    !File.Exists(Path.Combine(context.BaseDirectory, "NanoLink.sln")))
{
    context.BaseDirectory = Directory.GetCurrentDirectory();
}

context.ArtifactsDirectory = Path.Combine(context.BaseDirectory, "artifacts");

var pipeline = new SdlcPipeline();
bool success = await pipeline.RunAsync(context);

return success ? 0 : 1;
