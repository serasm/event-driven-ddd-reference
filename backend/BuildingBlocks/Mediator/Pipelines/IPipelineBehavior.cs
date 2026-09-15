using Mediator.Requests;

namespace Mediator.Pipelines;

public interface IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<Task<TResponse>> next);
}