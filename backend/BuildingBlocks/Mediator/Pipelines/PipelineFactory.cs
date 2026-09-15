using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Mediator.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Pipelines;

public class PipelineFactory : IPipelineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Delegate> _cache = new();
    
    private static readonly MethodInfo BuildMethod = 
        typeof(PipelineFactory)
            .GetMethod(
                nameof(Build),
                BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Method 'Build' not found");
    
    public PipelineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    public Delegate GetOrAdd<TResponse>(
        Type requestType,
        Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> handlerInvoker)
    {
        return _cache.GetOrAdd(
            (requestType, typeof(TResponse)),
            _ => BuildClosedPipeline<TResponse>(requestType, handlerInvoker));
    }

    private Delegate BuildClosedPipeline<TResponse>(
        Type requestType,
        Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> handlerInvoker)
    {
        var method = BuildMethod.MakeGenericMethod(
            requestType,
            typeof(TResponse));
        
        return (Delegate)method.Invoke(this, new object[] { handlerInvoker });
    }

    private Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> Build<TRequest, TResponse>(
        Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> handlerInvoker)
        where TRequest : IRequest<TResponse>
    {
        var pipelines = _serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        return (handlerObject, request, cancellationToken) =>
        {
            Func<Task<TResponse>> next = async () =>
                await handlerInvoker.Invoke(handlerObject, request, cancellationToken);

            foreach (var pipeline in pipelines.Reverse())
            {
                var current = next;

                next = () =>
                    pipeline.HandleAsync(
                        (TRequest)request,
                        cancellationToken,
                        current);
            }

            return next();
        };
    }
}