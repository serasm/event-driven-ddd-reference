using Mediator.Requests;

namespace Mediator.UnitTests;

public record TestRequest(int value) : IRequest<int> {}

public class TestRequestHandler : IRequestHandler<TestRequest, int>
{
    public CancellationToken CancellationToken { get; private set; }
    
    public Task<int> HandleAsync(TestRequest query, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        return Task.FromResult(query.value * 2);
    }
}