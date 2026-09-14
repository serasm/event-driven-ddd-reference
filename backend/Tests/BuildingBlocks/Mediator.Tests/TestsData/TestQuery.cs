namespace Mediator.Tests;

public sealed record TestQuery(int value) : IQuery<int>;

public sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    public CancellationToken CancellationToken { get; private set; }
    
    public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        return Task.FromResult(query.value * 2);
    }
}
