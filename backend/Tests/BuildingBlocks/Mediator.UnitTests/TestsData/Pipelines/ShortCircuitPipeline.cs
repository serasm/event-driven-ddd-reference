using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class ShortCircuitPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    public Task<int> HandleAsync(PipelineTestRequest request, CancellationToken cancellationToken, Func<Task<int>> next)
    {
        return Task.FromResult(123);
    }
}