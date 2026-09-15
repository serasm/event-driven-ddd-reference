using Mediator.Requests;

namespace Mediator.UnitTests.Requests;

public record ConcreteRequest(int value) : BaseRequest(value);

public class ConcreteRequestHandler : IRequestHandler<ConcreteRequest, int>
{
    public Task<int> HandleAsync(ConcreteRequest query, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.value * 2);
    }
}
