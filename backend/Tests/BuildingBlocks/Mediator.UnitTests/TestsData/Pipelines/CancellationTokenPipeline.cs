using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class CancellationTokenPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    private readonly PipelineTestContext _ctx;

    public CancellationTokenPipeline(PipelineTestContext ctx)
    {
        _ctx = ctx;
    }
    
    public Task<int> HandleAsync(PipelineTestRequest request, CancellationToken cancellationToken, Func<Task<int>> next)
    {
        _ctx.CancellationToken = cancellationToken;
        
        return next();
    }
}