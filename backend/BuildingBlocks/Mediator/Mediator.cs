using System.Collections.Concurrent;
using System.Linq.Expressions;
using Mediator.Pipelines;
using Mediator.Requests;

namespace Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPipelineFactory _pipelineFactory;

    private readonly ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)>
        _requestHandlerInvokers = new();

    public Mediator(IServiceProvider serviceProvider, IPipelineFactory pipelineFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
    }
    
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> query, CancellationToken cancellationToken = default)
    {
        var requestType = query.GetType();
        var (handlerType, invokerObj) = _requestHandlerInvokers.GetOrAdd(requestType, BuildRequestHandlerInfo<TResponse>);
        
        var invoker = (Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>)invokerObj;
        
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for type {handlerType.Name}");

        var pipelineInvoker =
            (Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>)_pipelineFactory.GetOrAdd(
                requestType, invoker);
        
        return pipelineInvoker(handler, query, cancellationToken);
    }
    
    private static (Type QueryHandlerType, Delegate Invoker) BuildRequestHandlerInfo<TResponse>(Type requestType)
    {
        var responseType = typeof(TResponse);
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handleMethod = handlerType.GetMethod("HandleAsync") 
                           ?? throw new InvalidOperationException($"No handle method registered for type {handlerType.Name}");
        
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(IRequest<TResponse>), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var body = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            handleMethod,
            Expression.Convert(requestParam, requestType),
            ctParam);
        
        var lambda = Expression.Lambda<Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>>(
            body, handlerParam, requestParam, ctParam);

        return (handlerType, lambda.Compile());
    }
}