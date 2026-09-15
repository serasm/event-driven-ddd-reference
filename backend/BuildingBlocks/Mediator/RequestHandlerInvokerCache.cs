using System.Collections.Concurrent;

namespace Mediator;

public sealed class RequestHandlerInvokerCache
{
    private readonly ConcurrentDictionary<
        Type,
        (Type RequestHandlerType, Delegate Invoker)> _cache = new();

    public (Type RequestHandlerType, Delegate Invoker) GetOrAdd(
        Type requestType,
        Func<Type, (Type RequestHandlerType, Delegate Invoker)> factory)
    {
        return _cache.GetOrAdd(requestType, factory);
    }
}