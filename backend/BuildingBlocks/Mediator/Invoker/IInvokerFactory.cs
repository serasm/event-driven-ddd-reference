namespace Mediator.Invoker;

public interface IInvokerFactory
{
    Delegate CreateHandler(Type queryType, Type handlerType);
}