using Mediator.Mediator;
using Mediator.Pipelines;
using Mediator.Requests;
using Mediator.UnitTests.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.UnitTests;

public sealed class PipelineFactoryTests
{
     [Fact]
    public async Task GetOrAdd_ShouldBuildWorkingPipeline()
    {
        var context = new PipelineTestContext();
        var services = new ServiceCollection();

        services.AddSingleton(context);

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            RecordingPipeline>();

        await using var provider = services.BuildServiceProvider();

        var factory = new PipelineFactory();

        var handlerInvoker =
            (object handler,
             IRequest<int> request,
             CancellationToken cancellationToken) =>
            {
                context.Trace.Add("Handler");
                return Task.FromResult(42);
            };

        var pipeline = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var result = await ((Func<
            IServiceProvider,
            object,
            IRequest<int>,
            CancellationToken,
            Task<int>>)pipeline)(
                provider,
                new object(),
                new PipelineTestRequest(),
                CancellationToken.None);

        Assert.Equal(42, result);

        Assert.Equal(
            new[]
            {
                "Pipeline.Before",
                "Handler",
                "Pipeline.After"
            },
            context.Trace);
    }

    [Fact]
    public async Task GetOrAdd_ShouldExecutePipelinesInRegistrationOrder()
    {
        var context = new PipelineTestContext();
        var services = new ServiceCollection();

        services.AddSingleton(context);

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            LoggingPipeline>();

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            ValidationPipeline>();

        await using var provider = services.BuildServiceProvider();

        var factory = new PipelineFactory();

        var handlerInvoker =
            (object handler,
             IRequest<int> request,
             CancellationToken cancellationToken) =>
            {
                context.Trace.Add("Handler");
                return Task.FromResult(42);
            };

        var pipeline = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var result = await ((Func<
            IServiceProvider,
            object,
            IRequest<int>,
            CancellationToken,
            Task<int>>)pipeline)(
                provider,
                new object(),
                new PipelineTestRequest(),
                CancellationToken.None);

        Assert.Equal(42, result);

        Assert.Equal(
            new[]
            {
                "Logging.Before",
                "Validation.Before",
                "Handler",
                "Validation.After",
                "Logging.After"
            },
            context.Trace);
    }

    [Fact]
    public async Task GetOrAdd_ShouldExecuteHandler_WhenThereAreNoPipelines()
    {
        var handlerCalled = false;

        var services = new ServiceCollection();

        using var provider = services.BuildServiceProvider();

        var factory = new PipelineFactory();

        var handlerInvoker =
            (object handler,
             IRequest<int> request,
             CancellationToken cancellationToken) =>
            {
                handlerCalled = true;
                return Task.FromResult(42);
            };

        var pipeline = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var result = await ((Func<
            IServiceProvider,
            object,
            IRequest<int>,
            CancellationToken,
            Task<int>>)pipeline)(
                provider,
                new object(),
                new PipelineTestRequest(),
                CancellationToken.None);

        Assert.Equal(42, result);
        Assert.True(handlerCalled);
    }

    [Fact]
    public void GetOrAdd_ShouldReturnSameDelegate_ForSameRequestAndResponseType()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var factory = new PipelineFactory();

        Func<object, IRequest<int>, CancellationToken, Task<int>> handlerInvoker =
            (handler, request, cancellationToken) =>
                Task.FromResult(42);

        var first = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var second = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrAdd_ShouldCreateSeparateDelegates_ForDifferentRequestTypes()
    {
        var services = new ServiceCollection();
        
        using var provider = services.BuildServiceProvider();

        var factory = new PipelineFactory();

        Func<object, IRequest<int>, CancellationToken, Task<int>> firstInvoker =
            (handler, request, cancellationToken) =>
                Task.FromResult(42);

        Func<object, IRequest<string>, CancellationToken, Task<string>> secondInvoker =
            (handler, request, cancellationToken) =>
                Task.FromResult("42");

        var first = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            firstInvoker);

        var second = factory.GetOrAdd(
            typeof(PipelineOtherTestRequest),
            secondInvoker);

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task Pipeline_ShouldBeAbleToShortCircuitRequest()
    {
        var handlerCalled = false;
        var services = new ServiceCollection();
        
        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            ShortCircuitPipeline>();

        await using var provider = services.BuildServiceProvider();
        var factory = new PipelineFactory();

        var handlerInvoker =
            (object handler,
             IRequest<int> request,
             CancellationToken cancellationToken) =>
            {
                handlerCalled = true;
                return Task.FromResult(42);
            };

        var pipeline = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var result = await ((Func<
            IServiceProvider,
            object,
            IRequest<int>,
            CancellationToken,
            Task<int>>)pipeline)(
                provider,
                new object(),
                new PipelineTestRequest(),
                CancellationToken.None);

        Assert.Equal(123, result);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Pipeline_ShouldReceiveCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var services = new ServiceCollection();
        var context = new PipelineTestContext();

        services.AddSingleton(context);

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            CancellationTokenPipeline>();

        await using var provider = services.BuildServiceProvider();
        var factory = new PipelineFactory();

        var handlerInvoker =
            (object handler,
             IRequest<int> request,
             CancellationToken cancellationToken) =>
                Task.FromResult(42);

        var pipeline = factory.GetOrAdd(
            typeof(PipelineTestRequest),
            handlerInvoker);

        var result = await ((Func<
            IServiceProvider,
            object,
            IRequest<int>,
            CancellationToken,
            Task<int>>)pipeline)(
                provider,
                new object(),
                new PipelineTestRequest(),
                cts.Token);

        Assert.Equal(42, result);

        var behavior =
            provider.GetRequiredService<
                IPipelineBehavior<PipelineTestRequest, int>>();

        Assert.IsType<CancellationTokenPipeline>(behavior);
        Assert.Equal(cts.Token, context.CancellationToken);
    }
    
    [Fact]
    public async Task SendAsync_ShouldExecutePipelineInExpectedOrder()
    {
        var context = new PipelineTestContext();
        var services = new ServiceCollection();

        services.AddSingleton(context);

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            LoggingPipeline>();

        services.AddTransient<
            IPipelineBehavior<PipelineTestRequest, int>,
            ValidationPipeline>();

        services.AddTransient<
            IRequestHandler<PipelineTestRequest, int>,
            PipelineTestRequestHandler>();

        services.AddTransient<IPipelineFactory, PipelineFactory>();
        services.AddScoped<IMediator, Mediator.Mediator>();
        services.AddSingleton(new RequestHandlerInvokerCache());

        await using var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(
            new PipelineTestRequest(),
            CancellationToken.None);

        Assert.Equal(42, result);

        Assert.Equal(
            new[]
            {
                "Logging.Before",
                "Validation.Before",
                "Handler",
                "Validation.After",
                "Logging.After"
            },
            context.Trace);
    }
}