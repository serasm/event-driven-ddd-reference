namespace Mediator.Tests.Inheritance;

public sealed record ConcreteQuery(int value) : BaseQuery(value);

public sealed class ConcreteQueryHandler : IQueryHandler<ConcreteQuery, int>
{
    public Task<int> HandleAsync(ConcreteQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.value * 2);
    }
}