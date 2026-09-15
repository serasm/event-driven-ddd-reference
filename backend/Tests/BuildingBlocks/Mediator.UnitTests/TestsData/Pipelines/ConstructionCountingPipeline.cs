using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class ConstructionCountingPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    public static int ConstructionCount = 0;

    public ConstructionCountingPipeline()
    {
        Interlocked.Increment(ref ConstructionCount);
    }
    
    public Task<int> HandleAsync(PipelineTestRequest request, CancellationToken cancellationToken, Func<Task<int>> next)
    {
        return next();
    }
}