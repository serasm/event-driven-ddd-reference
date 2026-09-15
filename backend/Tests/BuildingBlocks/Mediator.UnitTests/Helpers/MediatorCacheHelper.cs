using System.Collections.Concurrent;
using System.Reflection;

namespace Mediator.UnitTests.Helpers;

public static class MediatorCacheHelper
{
    public static ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)> GetRequestHandlerCache(
        RequestHandlerInvokerCache requestHandlerInvokerCache)
    {
        var field = typeof(RequestHandlerInvokerCache).GetField(
            "_cache",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        
        var value = field!.GetValue(requestHandlerInvokerCache);
        Assert.NotNull(value);
        
        return Assert.IsType<ConcurrentDictionary<Type, (Type RequestHandlerType, Delegate Invoker)>>(value);
    }
}