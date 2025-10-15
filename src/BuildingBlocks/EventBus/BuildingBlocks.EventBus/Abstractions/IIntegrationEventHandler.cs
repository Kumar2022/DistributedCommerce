namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// Base interface for integration event handlers
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>
    /// Handle an integration event
    /// </summary>
    Task HandleAsync(string eventData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed handler for integration events
/// </summary>
/// <typeparam name="TEvent">The event type to handle</typeparam>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    /// <summary>
    /// Handle a strongly-typed integration event
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);

    // Default implementation for base interface
    async Task IIntegrationEventHandler.HandleAsync(string eventData, CancellationToken cancellationToken)
    {
        var @event = System.Text.Json.JsonSerializer.Deserialize<TEvent>(eventData);
        if (@event is not null)
        {
            await HandleAsync(@event, cancellationToken);
        }
    }
}
