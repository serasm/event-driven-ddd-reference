using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class LoggingPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    private readonly PipelineTestContext _testContext;
    
    public LoggingPipeline(PipelineTestContext testContext)
    {
        _testContext = testContext;
    }

    public async Task<int> HandleAsync(
        PipelineTestRequest request,
        CancellationToken cancellationToken,
        Func<Task<int>> next)
    {
        _testContext.Trace.Add("Logging.Before");
        var result = await next();
        _testContext.Trace.Add("Logging.After");
        return result;
    }
}