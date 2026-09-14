namespace Mediator.Tests.Inheritance;

public abstract record BaseQuery(int value) : IQuery<int>;

public sealed class BaseQueryHandler
    : IQueryHandler<BaseQuery, int>
{
    public Task<int> HandleAsync(
        BaseQuery query,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(999);
    }
}