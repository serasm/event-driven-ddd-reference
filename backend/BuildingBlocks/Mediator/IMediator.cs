namespace Mediator;

public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}