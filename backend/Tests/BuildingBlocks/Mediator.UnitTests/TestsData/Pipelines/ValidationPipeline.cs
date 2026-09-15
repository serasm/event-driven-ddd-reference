using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class ValidationPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    private readonly PipelineTestContext _testContext;

    public ValidationPipeline(PipelineTestContext testContext)
    {
        _testContext = testContext;
    }
    
    public async Task<int> HandleAsync(PipelineTestRequest request, CancellationToken cancellationToken, Func<Task<int>> next)
    {
        _testContext.Trace.Add("Validation.Before");
        var result = await next();
        _testContext.Trace.Add("Validation.After");
        return result;
    }
}