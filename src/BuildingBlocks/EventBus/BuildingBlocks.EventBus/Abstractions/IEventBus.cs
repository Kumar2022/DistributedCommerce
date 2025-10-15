namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// Event bus for publishing and subscribing to integration events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish a single integration event to the message broker
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    /// <summary>
    /// Publish multiple integration events in batch
    /// </summary>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
