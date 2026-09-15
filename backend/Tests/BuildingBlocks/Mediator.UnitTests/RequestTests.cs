using Mediator.Pipelines;
using Mediator.Requests;
using Mediator.UnitTests.Helpers;
using Mediator.UnitTests.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.UnitTests;

public sealed class RequestTests
{
    [Fact]
    public async Task SendAsync_Request_ShouldCreateAndCacheInvoker()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        Assert.Empty(cache);
        
        var result = await mediator.SendAsync(new TestRequest(10));
        
        Assert.Equal(20, result);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestRequest)));
        
        var entry = cache[typeof(TestRequest)];
        Assert.Equal(
            typeof(IRequestHandler<TestRequest, int>),
            entry.RequestHandlerType);
        Assert.NotNull(entry.Invoker);
        Assert.IsType<Func<object, IRequest<int>, CancellationToken, Task<int>>>(entry.Invoker);
    }

    [Fact]
    public async Task SendAsync_Request_ShouldCreateWorkingInvoker()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        
        var mediatorResult = await mediator.SendAsync(new TestRequest(10));
        Assert.Equal(20, mediatorResult);
        
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestRequest)));   
        var entry = cache[typeof(TestRequest)];
        Assert.Equal(
            typeof(IRequestHandler<TestRequest, int>),
            entry.RequestHandlerType);
        var invoker = Assert.IsType<Func<object, IRequest<int>, CancellationToken, Task<int>>>(entry.Invoker);
        
        var handler = new TestRequestHandler();
        var result = await invoker(
            handler,
            new TestRequest(21),
            CancellationToken.None);
        Assert.Equal(42, result);
    }
    
    [Fact]
    public async Task SendAsync_Request_ShouldReuseCachedInvoker()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());

        await mediator.SendAsync(new TestRequest(1));
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        var firstEntry = cache[typeof(TestRequest)];

        await mediator.SendAsync(new TestRequest(2));
        var secondEntry = cache[typeof(TestRequest)];

        Assert.Same(firstEntry.Invoker, secondEntry.Invoker);
        Assert.Same(firstEntry.RequestHandlerType, secondEntry.RequestHandlerType);
    }
    
    [Fact]
    public async Task SendAsync_Request_ShouldResolveHandlerUsingConcreteRuntimeType()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<
            IRequestHandler<ConcreteRequest, int>,
            ConcreteRequestHandler>();
        services.AddSingleton<
            IRequestHandler<BaseRequest, int>,
            BaseRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        BaseRequest Request = new ConcreteRequest(21);

        var result = await mediator.SendAsync(Request);

        Assert.Equal(42, result);
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        Assert.Single(cache);
        Assert.True(
            cache.ContainsKey(typeof(ConcreteRequest)));
        Assert.False(
            cache.ContainsKey(typeof(BaseRequest)));
        var entry = cache[typeof(ConcreteRequest)];
        Assert.Equal(
            typeof(IRequestHandler<ConcreteRequest, int>),
            entry.RequestHandlerType);
    }
    
    [Fact]
    public async Task SendAsync_ShouldThrownAnException_WhenThereIsNoRequestHandler()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestRequest(10)));
        Assert.Contains("No handler registered for type ", result.Message);
    }

    [Fact]
    public async Task SendAsync_Request_ShouldPassCancellationTokenToHandler()
    {
        var handler = new TestRequestHandler();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        var ct = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<TestRequest, int>>(handler);
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        
        var result = await mediator.SendAsync(new TestRequest(10), ct.Token);
        
        Assert.Equal(ct.Token, handler.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_Request_ShouldCacheInvokersSeparatelyForDifferentRequestTypes()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        services.AddSingleton<IRequestHandler<ConcreteRequest, int>, ConcreteRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        
        await mediator.SendAsync(new TestRequest(1));
        await mediator.SendAsync(new ConcreteRequest(2));
        
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        Assert.Equal(2, cache.Count);
        Assert.True(cache.ContainsKey(typeof(TestRequest)));
        Assert.True(cache.ContainsKey(typeof(ConcreteRequest)));
    }
    
    [Fact]
    public async Task SendAsync_Request_ShouldPropagateHandlerException()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestExceptionRequest, int>, TestExceptionRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestExceptionRequest(1)));
        
        Assert.Contains("Request test exception", result.Message);
    }
    
    [Fact]
    public async Task SendAsync_Request_ShouldHandleConcurrentFirstCalls()
    {
        var services = new ServiceCollection();
        var requestHandlerInvokerCache = new RequestHandlerInvokerCache();
        services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator.Mediator(provider, requestHandlerInvokerCache, new PipelineFactory());
        var tasks = Enumerable.Range(0, 100)
            .Select(i => mediator.SendAsync(new TestRequest(i), CancellationToken.None));

        await Task.WhenAll(tasks);
        
        var cache = MediatorCacheHelper.GetRequestHandlerCache(requestHandlerInvokerCache);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestRequest)));
    }
}