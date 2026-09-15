using Mediator.Requests;

namespace Mediator.UnitTests.Requests;

public record BaseRequest(int value) : IRequest<int>;

public class BaseRequestHandler : IRequestHandler<BaseRequest, int>
{
    public Task<int> HandleAsync(BaseRequest query, CancellationToken cancellationToken)
    {
        return Task.FromResult(999);
    }
}