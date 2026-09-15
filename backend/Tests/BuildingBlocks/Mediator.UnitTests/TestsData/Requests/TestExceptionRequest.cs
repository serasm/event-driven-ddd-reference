using Mediator.Requests;

namespace Mediator.UnitTests.Requests;

public record TestExceptionRequest(int value) : IRequest<int>;

public class TestExceptionRequestHandler : IRequestHandler<TestExceptionRequest, int>
{
    public Task<int> HandleAsync(TestExceptionRequest query, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Request test exception");
    }
}