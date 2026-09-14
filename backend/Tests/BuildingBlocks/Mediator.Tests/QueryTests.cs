using Mediator.Tests.Helpers;
using Mediator.Tests.Inheritance;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class QueryTests
{
    [Fact]
    public async Task SendAsync_Query_ShouldCreateAndCacheInvoker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        Assert.Empty(cache);
        
        var result = await mediator.SendAsync(new TestQuery(10));
        
        Assert.Equal(20, result);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestQuery)));
        
        var entry = cache[typeof(TestQuery)];
        Assert.Equal(
            typeof(IQueryHandler<TestQuery, int>),
            entry.QueryHandlerType);
        Assert.NotNull(entry.Invoker);
        Assert.IsType<Func<object, IQuery<int>, CancellationToken, Task<int>>>(entry.Invoker);
    }

    [Fact]
    public async Task SendAsync_Query_ShouldCreateWorkingInvoker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var mediatorResult = await mediator.SendAsync(new TestQuery(10));
        Assert.Equal(20, mediatorResult);
        
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestQuery)));   
        var entry = cache[typeof(TestQuery)];
        Assert.Equal(
            typeof(IQueryHandler<TestQuery, int>),
            entry.QueryHandlerType);
        var invoker = Assert.IsType<Func<object, IQuery<int>, CancellationToken, Task<int>>>(entry.Invoker);
        
        var handler = new TestQueryHandler();
        var result = await invoker(
            handler,
            new TestQuery(21),
            CancellationToken.None);
        Assert.Equal(42, result);
    }
    
    [Fact]
    public async Task SendAsync_Query_ShouldReuseCachedInvoker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);

        await mediator.SendAsync(new TestQuery(1));
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        var firstEntry = cache[typeof(TestQuery)];

        await mediator.SendAsync(new TestQuery(2));
        var secondEntry = cache[typeof(TestQuery)];

        Assert.Same(firstEntry.Invoker, secondEntry.Invoker);
        Assert.Same(firstEntry.QueryHandlerType, secondEntry.QueryHandlerType);
    }
    
    [Fact]
    public async Task SendAsync_Query_ShouldResolveHandlerUsingConcreteRuntimeType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<
            IQueryHandler<ConcreteQuery, int>,
            ConcreteQueryHandler>();
        services.AddSingleton<
            IQueryHandler<BaseQuery, int>,
            BaseQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        BaseQuery query = new ConcreteQuery(21);

        var result = await mediator.SendAsync(query);

        Assert.Equal(42, result);
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        Assert.Single(cache);
        Assert.True(
            cache.ContainsKey(typeof(ConcreteQuery)));
        Assert.False(
            cache.ContainsKey(typeof(BaseQuery)));
        var entry = cache[typeof(ConcreteQuery)];
        Assert.Equal(
            typeof(IQueryHandler<ConcreteQuery, int>),
            entry.QueryHandlerType);
    }
    
    [Fact]
    public async Task SendAsync_ShouldThrownAnException_WhenThereIsNoQueryHandler()
    {
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestQuery(10)));
        Assert.Contains("No handler registered for type ", result.Message);
    }

    [Fact]
    public async Task SendAsync_Query_ShouldPassCancellationTokenToHandler()
    {
        var handler = new TestQueryHandler();
        var ct = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>>(handler);
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var result = await mediator.SendAsync(new TestQuery(10), ct.Token);
        
        Assert.Equal(ct.Token, handler.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_Query_ShouldCacheInvokersSeparatelyForDifferentRequestTypes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddSingleton<IQueryHandler<ConcreteQuery, int>, ConcreteQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        await mediator.SendAsync(new TestQuery(1));
        await mediator.SendAsync(new ConcreteQuery(2));
        
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        Assert.Equal(2, cache.Count);
        Assert.True(cache.ContainsKey(typeof(TestQuery)));
        Assert.True(cache.ContainsKey(typeof(ConcreteQuery)));
    }
    
    [Fact]
    public async Task SendAsync_Query_ShouldPropagateHandlerException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestExceptionQuery, int>, TestExceptionQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestExceptionQuery(1)));
        
        Assert.Contains("Query test exception", result.Message);
    }
    
    [Fact]
    public async Task SendAsync_Query_ShouldHandleConcurrentFirstCalls()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        var tasks = Enumerable.Range(0, 100)
            .Select(i => mediator.SendAsync(new TestQuery(i), CancellationToken.None));

        await Task.WhenAll(tasks);
        
        var cache = MediatorCacheHelper.GetQueryHandlerCache(mediator);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestQuery)));
    }
}