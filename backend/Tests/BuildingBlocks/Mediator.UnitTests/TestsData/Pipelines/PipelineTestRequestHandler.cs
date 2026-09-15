using Mediator.Requests;

namespace Mediator.UnitTests.Pipelines;

public sealed class PipelineTestRequestHandler : IRequestHandler<PipelineTestRequest, int>
{
    private readonly PipelineTestContext _testContext;

    public PipelineTestRequestHandler(PipelineTestContext testContext)
    {
        _testContext = testContext;
    }
    
    public Task<int> HandleAsync(PipelineTestRequest query, CancellationToken cancellationToken)
    {
        _testContext.Trace.Add("Handler");
        
        return Task.FromResult(42);
    }
}