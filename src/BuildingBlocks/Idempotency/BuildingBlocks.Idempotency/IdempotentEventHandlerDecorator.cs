using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Idempotency;

/// <summary>
/// Decorator that wraps event handlers to provide idempotency
/// Ensures event handlers are executed only once even if event is received multiple times
/// Critical pattern for FAANG-scale distributed systems
/// </summary>
/// <typeparam name="TEvent">Type of event being handled</typeparam>
public class IdempotentEventHandlerDecorator<TEvent>(
    IIntegrationEventHandler<TEvent> inner,
    IIdempotencyStore idempotencyStore,
    ILogger<IdempotentEventHandlerDecorator<TEvent>> logger)
    : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly IIntegrationEventHandler<TEvent> _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IIdempotencyStore _idempotencyStore = idempotencyStore ?? throw new ArgumentNullException(nameof(idempotencyStore));
    private readonly ILogger<IdempotentEventHandlerDecorator<TEvent>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var idempotencyKey = GetIdempotencyKey(@event);

        _logger.LogDebug(
            "Checking idempotency for event {EventType} with key {IdempotencyKey}",
            typeof(TEvent).Name,
            idempotencyKey);

        // Check if this event has already been processed
        var isProcessed = await _idempotencyStore.IsProcessedAsync(idempotencyKey, cancellationToken);

        if (isProcessed)
        {
            _logger.LogWarning(
                "Event {EventType} with ID {EventId} and idempotency key {IdempotencyKey} has already been processed. Skipping.",
                typeof(TEvent).Name,
                @event.EventId,
                idempotencyKey);
            
            return; // Event already processed, skip
        }

        try
        {
            _logger.LogInformation(
                "Processing event {EventType} with ID {EventId} for the first time",
                typeof(TEvent).Name,
                @event.EventId);

            // Execute the actual event handler
            await _inner.HandleAsync(@event, cancellationToken);

            // Mark as processed after successful execution
            await _idempotencyStore.MarkAsProcessedAsync(
                idempotencyKey,
                result: null, // Can store result if needed
                ttl: TimeSpan.FromHours(24), // Keep for 24 hours
                cancellationToken);

            _logger.LogInformation(
                "Successfully processed and marked event {EventType} with ID {EventId} as processed",
                typeof(TEvent).Name,
                @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing event {EventType} with ID {EventId}. Event will NOT be marked as processed for retry.",
                typeof(TEvent).Name,
                @event.EventId);

            // Don't mark as processed on failure - allows retry
            throw;
        }
    }

    /// <summary>
    /// Generate idempotency key from event
    /// Uses EventId as the primary key, can be extended to include other fields
    /// </summary>
    private string GetIdempotencyKey(TEvent @event)
    {
        // Use EventId + EventType for idempotency
        // This ensures each unique event is processed exactly once
        return $"{typeof(TEvent).Name}:{@event.EventId}";
    }
}

/// <summary>
/// Base interface for integration event handlers
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for integration events
/// All integration events must have a unique EventId for idempotency
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// Used for idempotency tracking
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// Timestamp when event was created
    /// </summary>
    DateTime CreatedAt { get; }
}
