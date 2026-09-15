using System.Collections.Concurrent;
using System.Reflection;

namespace Mediator.UnitTests.Helpers;

public static class MediatorCacheHelper
{
    public static ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)> GetRequestHandlerCache(
        IMediator mediator)
    {
        var field = typeof(Mediator).GetField(
            "_requestHandlerInvokers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        
        var value = field!.GetValue(mediator);
        Assert.NotNull(value);
        
        return Assert.IsType<ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)>>(value);
    }
}