namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Represents a message in the outbox for reliable event publishing
/// Implements the Transactional Outbox Pattern for at-least-once delivery
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The type of the domain event (e.g., "OrderCreatedEvent")
    /// </summary>
    public string EventType { get; init; } = string.Empty;
    
    /// <summary>
    /// Serialized JSON payload of the event
    /// </summary>
    public string Payload { get; init; } = string.Empty;
    
    /// <summary>
    /// When the domain event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the message was successfully published to the message broker
    /// Null means not yet published
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Error message if publishing failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Number of times we've attempted to publish this message
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; init; }
    
    /// <summary>
    /// Aggregate ID that triggered this event (for debugging)
    /// </summary>
    public Guid? AggregateId { get; init; }
}
