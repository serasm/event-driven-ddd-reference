using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Type, (Type QueryHandlerType, Delegate Invoker)>
        _queryHandlerInvokers = new();
    private readonly ConcurrentDictionary<Type, (Type CommandHandlerType, Delegate Invoker)>
        _commandHandlerInvokers = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        if(serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        
        _serviceProvider = serviceProvider;
    }
    
    public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var requestType = query.GetType();
        var (handlerType, invokerObj) = _queryHandlerInvokers.GetOrAdd(requestType, BuildQueryHandlerInfo<TResponse>);
        
        var invoker = (Func<object, IQuery<TResponse>, CancellationToken, Task<TResponse>>)invokerObj;
        
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for type {handlerType.Name}");
        
        return invoker(handler, query, cancellationToken);
    }

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var requestType = command.GetType();
        var (handlerType, invokerObj) = _commandHandlerInvokers.GetOrAdd(requestType, BuildCommandHandlerInfo);
        
        var invoker = (Func<object, ICommand, CancellationToken, Task>)invokerObj;
        
        var handler = _serviceProvider.GetService(handlerType)
                      ?? throw new InvalidOperationException($"No handler registered for type {handlerType.Name}");
        
        return invoker(handler, command, cancellationToken);
    }

    private static (Type QueryHandlerType, Delegate Invoker) BuildQueryHandlerInfo<TResponse>(Type requestType)
    {
        var responseType = typeof(TResponse);
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType);
        var handleMethod = handlerType.GetMethod("HandleAsync") 
            ?? throw new InvalidOperationException($"No handle method registered for type {handlerType.Name}");
        
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(IQuery<TResponse>), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var body = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            ctParam);
        
        var lambda = Expression.Lambda<Func<object, IQuery<TResponse>, CancellationToken, Task<TResponse>>>(
            body, handlerParam, requestParam, ctParam);

        return (handlerType, lambda.Compile());
    }
    
    private static (Type QueryHandlerType, Delegate Invoker) BuildCommandHandlerInfo(Type requestType)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(requestType);
        var handleMethod = handlerType.GetMethod("HandleAsync") 
                           ?? throw new InvalidOperationException($"No handle method registered for type {handlerType.Name}");
        
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(ICommand), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var body = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            ctParam);
        
        var lambda = Expression.Lambda<Func<object, ICommand, CancellationToken, Task>>(
            body, handlerParam, requestParam, ctParam);

        return (handlerType, lambda.Compile());
    }
}