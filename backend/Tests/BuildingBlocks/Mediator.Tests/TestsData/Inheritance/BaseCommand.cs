namespace Mediator.Tests.Inheritance;

public abstract record BaseCommand(int value) : ICommand;

public sealed class BaseCommandHandler : ICommandHandler<BaseCommand>
{
    public int ReceivedValue { get; private set; }
    
    public Task HandleAsync(BaseCommand command, CancellationToken cancellationToken)
    {
        ReceivedValue = command.value;
        
        return Task.CompletedTask;
    }
}
