using Mediator.Pipelines;

namespace Mediator.UnitTests.Pipelines;

public sealed class RecordingPipeline : IPipelineBehavior<PipelineTestRequest, int>
{
    private readonly PipelineTestContext _testContext;

    public RecordingPipeline(PipelineTestContext testContext)
    {
        _testContext = testContext;
    }

    public Task<int> HandleAsync(
        PipelineTestRequest request,
        CancellationToken cancellationToken,
        Func<Task<int>> next)
    {
        _testContext.Trace.Add("Pipeline.Before");
        return Handle();

        async Task<int> Handle()
        {
            var result = await next();
            _testContext.Trace.Add("Pipeline.After");
            return result;
        }
    }
}