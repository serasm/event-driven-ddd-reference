namespace Mediator;

public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    
}