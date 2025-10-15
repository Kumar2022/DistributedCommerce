namespace BuildingBlocks.Infrastructure.DeadLetterQueue;

/// <summary>
/// Represents a message that has permanently failed processing and moved to DLQ
/// Used for manual intervention, debugging, and re-processing after fixes
/// </summary>
public sealed class DeadLetterMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The type of the event that failed (e.g., "OrderCreatedIntegrationEvent")
    /// </summary>
    public string EventType { get; init; } = string.Empty;
    
    /// <summary>
    /// Serialized JSON payload of the failed event
    /// </summary>
    public string Payload { get; init; } = string.Empty;
    
    /// <summary>
    /// When the event originally occurred
    /// </summary>
    public DateTime OriginalTimestamp { get; init; }
    
    /// <summary>
    /// When the message was moved to DLQ
    /// </summary>
    public DateTime MovedToDlqAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// The final error that caused permanent failure
    /// </summary>
    public string FailureReason { get; init; } = string.Empty;
    
    /// <summary>
    /// Stack trace or detailed error information
    /// </summary>
    public string? ErrorDetails { get; init; }
    
    /// <summary>
    /// Number of times processing was attempted before moving to DLQ
    /// </summary>
    public int TotalAttempts { get; init; }
    
    /// <summary>
    /// Service that failed to process this message
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;
    
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; init; }
    
    /// <summary>
    /// Original message ID (from inbox/outbox)
    /// </summary>
    public Guid? OriginalMessageId { get; init; }
    
    /// <summary>
    /// Whether this message has been reprocessed successfully
    /// </summary>
    public bool Reprocessed { get; set; }
    
    /// <summary>
    /// When the message was successfully reprocessed (if applicable)
    /// </summary>
    public DateTime? ReprocessedAt { get; set; }
    
    /// <summary>
    /// Notes from operations team about this failure
    /// </summary>
    public string? OperatorNotes { get; set; }
}
