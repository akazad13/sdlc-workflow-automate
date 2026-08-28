namespace NanoLink.Orchestrator.Core.Stages;

public interface ISdlcStage
{
    int StageNumber { get; }
    string Name { get; }
    string Description { get; }
    Task<bool> ExecuteAsync(SdlcContext context, CancellationToken ct = default);
}
