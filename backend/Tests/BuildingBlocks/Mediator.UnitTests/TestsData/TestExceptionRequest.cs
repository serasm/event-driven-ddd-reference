using Mediator.Requests;

namespace Mediator.UnitTests;

public record TestExceptionRequest(int value) : IRequest<int>;

public class TestExceptionRequestHandler : IRequestHandler<TestExceptionRequest, int>
{
    public Task<int> HandleAsync(TestExceptionRequest query, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Query test exception");
    }
}