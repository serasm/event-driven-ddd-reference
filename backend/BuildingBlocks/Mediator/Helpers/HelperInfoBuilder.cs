using System.Linq.Expressions;
using Mediator.Requests;

namespace Mediator.Helpers;

public static class HelperInfoBuilder
{
    public static (Type QueryHandlerType, Delegate Invoker) BuildRequestHandlerInfo<TResponse>(Type requestType)
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