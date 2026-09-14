namespace Mediator.UnitTests;

public sealed record TestCommand(int value) : ICommand;

public sealed class TestCommandHandler : ICommandHandler<TestCommand>
{
    public int ReceivedValue { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    
    public Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        ReceivedValue = command.value;
        CancellationToken = cancellationToken;
        
        return Task.CompletedTask;
    }
}
