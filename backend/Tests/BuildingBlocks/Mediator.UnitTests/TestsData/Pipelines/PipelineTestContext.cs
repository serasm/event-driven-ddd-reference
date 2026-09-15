namespace Mediator.UnitTests.Pipelines;

public sealed class PipelineTestContext
{
    public List<string> Trace { get; } = [];
    public CancellationToken CancellationToken { get; set; }
}