namespace AgentShop.SharedKernel;

/// <summary>
/// Marker for events published by one module and consumed by others.
/// Integration events live in the publishing module's *.Contracts project.
/// </summary>
#pragma warning disable CA1040 // Marker interface is intentional: it constrains what can be published.
public interface IIntegrationEvent;
#pragma warning restore CA1040

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}

/// <summary>
/// Publishes events in-process to every registered handler, sequentially, within the caller's DI scope.
/// Register as scoped so handlers share the request scope (and its DbContexts).
/// There is no outbox: an event is lost if the process dies after the commit (see docs/adr/0004).
/// </summary>
public sealed class InProcessIntegrationEventPublisher(IServiceProvider serviceProvider) : IIntegrationEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = (IEnumerable<IIntegrationEventHandler<TEvent>>?)serviceProvider.GetService(
            typeof(IEnumerable<IIntegrationEventHandler<TEvent>>)) ?? [];

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}
