namespace Mediator.Tests;

public sealed record TestExceptionQuery(int value) : IQuery<int>;

public sealed class TestExceptionQueryHandler : IQueryHandler<TestExceptionQuery, int>
{
    public Task<int> HandleAsync(TestExceptionQuery query, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Query test exception");
    }
}