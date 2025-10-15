namespace BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Represents a received message in the inbox for idempotent event processing
/// Ensures exactly-once processing of incoming events from external services
/// FAANG-scale pattern: Unique constraint on (EventId, Consumer) for idempotency
/// </summary>
public sealed class InboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The type of the received event (e.g., "OrderCreatedIntegrationEvent")
    /// </summary>
    public string EventType { get; init; } = string.Empty;
    
    /// <summary>
    /// Name of the consumer/handler that processes this message
    /// Used with EventId for unique constraint to allow same event to different consumers
    /// </summary>
    public string Consumer { get; init; } = string.Empty;
    
    /// <summary>
    /// Serialized JSON payload of the event
    /// </summary>
    public string Payload { get; init; } = string.Empty;
    
    /// <summary>
    /// When the event was received by this service
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the event was successfully processed
    /// Null means not yet processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Number of times we've attempted to process this message
    /// </summary>
    public int ProcessingAttempts { get; set; }
    
    /// <summary>
    /// Unique identifier for the originating event (for deduplication)
    /// This should be the event ID from the publishing service
    /// Used with Consumer for unique constraint
    /// </summary>
    public Guid EventId { get; init; }
    
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; init; }
}
