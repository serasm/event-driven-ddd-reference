namespace Mediator;

public interface ICommandHandler<TCommandType> where TCommandType : ICommand
{
    Task HandleAsync(TCommandType command, CancellationToken cancellationToken);
}