using Mediator.UnitTests.Helpers;
using Mediator.UnitTests.Inheritance;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.UnitTests;

public class CommandTests
{
    [Fact]
    public async Task SendAsync_Command_ShouldCreateAndCacheInvoker()
    {
        var services = new ServiceCollection();
        var handler = new TestCommandHandler();
        services.AddSingleton<ICommandHandler<TestCommand>>(handler);
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);        
        var cache = MediatorCacheHelper.GetCommandHandlerCache(mediator);
        Assert.Empty(cache);

        await mediator.SendAsync(new TestCommand(123), CancellationToken.None);

        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestCommand)));
        var entry = cache[typeof(TestCommand)];
        Assert.Equal(
            typeof(ICommandHandler<TestCommand>),
            entry.CommandHandlerType);

        var invoker =
            Assert.IsType<
                Func<object, ICommand, CancellationToken, Task>
            >(entry.Invoker);

        await invoker(
            handler,
            new TestCommand(456),
            CancellationToken.None);

        Assert.Equal(456, handler.ReceivedValue);
    }
    
    [Fact]
    public async Task SendAsync_Command_ShouldResolveHandlerUsingConcreteRuntimeType()
    {
        var services = new ServiceCollection();
        var handler = new ConcreteCommandHandler();
        services.AddSingleton<ICommandHandler<ConcreteCommand>>(handler);
        services.AddSingleton<
            ICommandHandler<BaseCommand>,
            BaseCommandHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        BaseCommand query = new ConcreteCommand(21);

        await mediator.SendAsync(query);
        
        var cache = MediatorCacheHelper.GetCommandHandlerCache(mediator);
        Assert.Single(cache);
        Assert.True(
            cache.ContainsKey(typeof(ConcreteCommand)));
        Assert.False(
            cache.ContainsKey(typeof(BaseCommand)));
        var entry = cache[typeof(ConcreteCommand)];
        Assert.Equal(
            typeof(ICommandHandler<ConcreteCommand>),
            entry.CommandHandlerType);

        var invoker =
            Assert.IsType<
                Func<object, ICommand, CancellationToken, Task>
            >(entry.Invoker);

        await invoker(
            handler,
            new ConcreteCommand(456),
            CancellationToken.None);

        Assert.Equal(456, handler.ReceivedValue);
    }
    
    [Fact]
    public async Task SendAsync_ShouldThrownAnException_WhenThereIsNoCommandHandler()
    {
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestCommand(10)));
        Assert.Contains("No handler registered for type ", result.Message);
    }
    
    [Fact]
    public async Task SendAsync_Command_ShouldPassCancellationTokenToHandler()
    {
        var handler = new TestCommandHandler();
        var ct = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<TestCommand>>(handler);
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        await mediator.SendAsync(new TestCommand(10), ct.Token);
        
        Assert.Equal(ct.Token, handler.CancellationToken);
    }
    
    [Fact]
    public async Task SendAsync_Command_ShouldCacheInvokersSeparatelyForDifferentRequestTypes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddSingleton<ICommandHandler<ConcreteCommand>, ConcreteCommandHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        await mediator.SendAsync(new TestCommand(1));
        await mediator.SendAsync(new ConcreteCommand(2));
        
        var cache = MediatorCacheHelper.GetCommandHandlerCache(mediator);
        Assert.Equal(2, cache.Count);
        Assert.True(cache.ContainsKey(typeof(TestCommand)));
        Assert.True(cache.ContainsKey(typeof(ConcreteCommand)));
    }

    [Fact]
    public async Task SendAsync_Command_ShouldPropagateHandlerException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<TestExceptionCommand>, TestExceptionCommandHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        
        var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await mediator.SendAsync(new TestExceptionCommand()));
        
        Assert.Contains("Command test exception", result.Message);
    }

    [Fact]
    public async Task SendAsync_Command_ShouldHandleConcurrentFirstCalls()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<TestCommand>, TestCommandHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = new Mediator(provider);
        var tasks = Enumerable.Range(0, 100)
            .Select(i => mediator.SendAsync(new TestCommand(i), CancellationToken.None));

        await Task.WhenAll(tasks);
        
        var cache = MediatorCacheHelper.GetCommandHandlerCache(mediator);
        Assert.Single(cache);
        Assert.True(cache.ContainsKey(typeof(TestCommand)));
    }
}