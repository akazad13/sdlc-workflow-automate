using NanoLink.Orchestrator.Core;

var context = SdlcContext.InitializeFromEnvironment(args);

var pipeline = new SdlcPipeline();
bool success = await pipeline.RunAsync(context);

return success ? 0 : 1;
