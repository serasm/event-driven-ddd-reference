namespace Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        if(serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        
        _serviceProvider = serviceProvider;
    }
    
    public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var requestType = query.GetType();
        var queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        var queryHandler = _serviceProvider.GetService(queryHandlerType);
        
        if(queryHandler == null)
            throw new InvalidOperationException($"No query handler registered for type {queryHandlerType.Name}");
        
        var handleMethod = queryHandlerType.GetMethod("HandleAsync");
        if(handleMethod == null)
            throw new InvalidOperationException($"No handle method registered for type {queryHandlerType.Name}");
        
        var resultTask = (Task<TResponse>)handleMethod.Invoke(queryHandler, new object[] { query, cancellationToken });

        return resultTask;
    }

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var requestType = command.GetType();
        var queryHandlerType = typeof(ICommandHandler<>).MakeGenericType(requestType);
        
        var queryHandler = _serviceProvider.GetService(queryHandlerType);
        
        if(queryHandler == null)
            throw new InvalidOperationException($"No query handler registered for type {queryHandlerType.Name}");
        
        var handleMethod = queryHandlerType.GetMethod("HandleAsync");
        if(handleMethod == null)
            throw new InvalidOperationException($"No handle method registered for type {queryHandlerType.Name}");
        
        var resultTask = (Task)handleMethod.Invoke(queryHandler, new object[] { command, cancellationToken });

        return resultTask;
    }
}