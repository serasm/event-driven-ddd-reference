namespace Mediator.UnitTests.Inheritance;

public sealed record ConcreteCommand(int Value) : BaseCommand(Value);

public sealed class ConcreteCommandHandler : ICommandHandler<ConcreteCommand>
{
    public int ReceivedValue { get; private set; }
    
    public Task HandleAsync(ConcreteCommand command, CancellationToken cancellationToken)
    {
        ReceivedValue = command.value;
        
        return Task.CompletedTask;
    }
}