namespace Mediator.Tests;

public sealed record TestExceptionCommand : ICommand;

public sealed class TestExceptionCommandHandler : ICommandHandler<TestExceptionCommand>
{
    public Task HandleAsync(TestExceptionCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Command test exception");
    }
}