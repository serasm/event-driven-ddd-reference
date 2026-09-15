namespace Mediator.Requests;

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest query, CancellationToken cancellationToken);
}