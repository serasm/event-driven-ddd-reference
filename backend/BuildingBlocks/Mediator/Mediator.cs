using System.Collections.Concurrent;
using System.Linq.Expressions;
using Mediator.Helpers;
using Mediator.Requests;

namespace Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)>
        _requestHandlerInvokers = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        if(serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        
        _serviceProvider = serviceProvider;
    }
    
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> query, CancellationToken cancellationToken = default)
    {
        var requestType = query.GetType();
        var (handlerType, invokerObj) = _requestHandlerInvokers.GetOrAdd(requestType, HelperInfoBuilder.BuildRequestHandlerInfo<TResponse>);
        
        var invoker = (Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>>)invokerObj;
        
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for type {handlerType.Name}");
        
        return invoker(handler, query, cancellationToken);
    }
}