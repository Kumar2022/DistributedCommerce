using System.Text.Json;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.Infrastructure.DeadLetterQueue;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Decorator that wraps event handlers with Inbox pattern for idempotent event processing
/// Ensures events are processed exactly once, even if they arrive multiple times
/// </summary>
/// <typeparam name="TEvent">The integration event type</typeparam>
public sealed class InboxEventHandlerDecorator<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly IIntegrationEventHandler<TEvent> _innerHandler;
    private readonly IInboxRepository _inboxRepository;
    private readonly IDeadLetterQueueService _deadLetterQueueService;
    private readonly ILogger _logger;

    public InboxEventHandlerDecorator(
        IIntegrationEventHandler<TEvent> innerHandler,
        IInboxRepository inboxRepository,
        IDeadLetterQueueService deadLetterQueueService,
        ILogger logger)
    {
        _innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
        _inboxRepository = inboxRepository ?? throw new ArgumentNullException(nameof(inboxRepository));
        _deadLetterQueueService = deadLetterQueueService ?? throw new ArgumentNullException(nameof(deadLetterQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        var eventId = @event.Id;
        var eventType = typeof(TEvent).Name;

        _logger.LogDebug(
            "Processing event {EventType} with ID {EventId}",
            eventType, eventId);

        // Check if we've already processed this event (idempotency check)
        var alreadyProcessed = await _inboxRepository.ExistsAsync(eventId, cancellationToken);
        
        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Event {EventType} with ID {EventId} has already been processed. Skipping (idempotency).",
                eventType, eventId);
            return; // Duplicate - skip processing
        }

        // Create inbox message to track processing
        var inboxMessage = new InboxMessage
        {
            EventId = eventId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(@event),
            ReceivedAt = DateTime.UtcNow,
            CorrelationId = @event.CorrelationId != null ? Guid.Parse(@event.CorrelationId) : null
        };

        // Add to inbox (atomic operation - will fail if duplicate due to unique constraint on EventId)
        var added = await _inboxRepository.AddAsync(inboxMessage, cancellationToken);
        
        if (!added)
        {
            _logger.LogWarning(
                "Event {EventType} with ID {EventId} was already added to inbox by another instance. Skipping.",
                eventType, eventId);
            return; // Another instance already processing
        }

        try
        {
            // Process the event with the inner handler
            _logger.LogInformation(
                "Processing event {EventType} with ID {EventId}",
                eventType, eventId);

            await _innerHandler.HandleAsync(@event, cancellationToken);

            // Mark as successfully processed
            await _inboxRepository.MarkAsProcessedAsync(inboxMessage.Id, cancellationToken);

            _logger.LogInformation(
                "Successfully processed event {EventType} with ID {EventId}",
                eventType, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing event {EventType} with ID {EventId}. Attempt {Attempt}",
                eventType, eventId, inboxMessage.ProcessingAttempts + 1);

            // Mark as failed
            await _inboxRepository.MarkAsFailedAsync(inboxMessage.Id, ex.Message, cancellationToken);

            // Check if we've exceeded max retry attempts
            const int maxAttempts = 3;
            if (inboxMessage.ProcessingAttempts >= maxAttempts)
            {
                _logger.LogError(
                    "Event {EventType} with ID {EventId} has failed {MaxAttempts} times. Moving to Dead Letter Queue.",
                    eventType, eventId, maxAttempts);

                // Move to Dead Letter Queue
                await _deadLetterQueueService.MoveToDeadLetterQueueAsync(
                    eventType,
                    inboxMessage.Payload,
                    "Max retry attempts exceeded",
                    ex.Message,
                    inboxMessage.ProcessingAttempts + 1,
                    inboxMessage.CorrelationId,
                    inboxMessage.Id,
                    inboxMessage.ReceivedAt,
                    cancellationToken);
            }

            // Re-throw to let Kafka consumer know processing failed (for retry/backoff)
            throw;
        }
    }
}
