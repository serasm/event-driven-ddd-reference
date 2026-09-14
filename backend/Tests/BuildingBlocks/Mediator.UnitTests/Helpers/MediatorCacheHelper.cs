using System.Collections.Concurrent;
using System.Reflection;

namespace Mediator.UnitTests.Helpers;

public static class MediatorCacheHelper
{
    public static ConcurrentDictionary<Type, (Type QueryHandlerType, Delegate Invoker)> GetQueryHandlerCache(
        IMediator mediator)
    {
        var field = typeof(Mediator).GetField(
            "_queryHandlerInvokers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        
        var value = field!.GetValue(mediator);
        Assert.NotNull(value);
        
        return Assert.IsType<ConcurrentDictionary<Type, (Type QueryHandlerType, Delegate Invoker)>>(value);
    }
    
    public static ConcurrentDictionary<
        Type,
        (Type CommandHandlerType, Delegate Invoker)
    > GetCommandHandlerCache(Mediator mediator)
    {
        var field = typeof(Mediator).GetField(
            "_commandHandlerInvokers",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        return Assert.IsType<
            ConcurrentDictionary<
                Type,
                (Type CommandHandlerType, Delegate Invoker)
            >
        >(field!.GetValue(mediator));
    }
}