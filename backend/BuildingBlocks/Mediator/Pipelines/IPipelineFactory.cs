using Mediator.Requests;

namespace Mediator.Pipelines;

public interface IPipelineFactory
{
    public Delegate GetOrAdd<TResponse>(
        Type requestType,
        Func<object, IRequest<TResponse>, CancellationToken, Task<TResponse>> handlerInvoker);
}