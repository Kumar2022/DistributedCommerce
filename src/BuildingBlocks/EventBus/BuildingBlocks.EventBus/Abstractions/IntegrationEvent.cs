namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// Base class for integration events exchanged between services
/// Includes support for idempotency, distributed tracing, and event sourcing (FAANG-scale requirements)
/// Implements standard envelope pattern with correlation/causation tracking
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance (used for idempotency via Inbox pattern)
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Identifier of the aggregate that produced this event
    /// </summary>
    public Guid AggregateId { get; init; }
    
    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Name of the event type (for routing and schema versioning)
    /// </summary>
    public string EventType { get; init; }
    
    /// <summary>
    /// Schema version of this event (for backward compatibility and evolution)
    /// Format: "1.0", "2.0", etc.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";
    
    /// <summary>
    /// Name of the service that produced this event (for routing and debugging)
    /// </summary>
    public string Producer { get; init; } = string.Empty;

    // Distributed Tracing Support (FAANG-scale observability)
    
    /// <summary>
    /// W3C Trace Context traceparent header for distributed tracing
    /// Format: "00-{traceId}-{spanId}-{flags}"
    /// https://www.w3.org/TR/trace-context/
    /// </summary>
    public string? Traceparent { get; init; }
    
    /// <summary>
    /// Distributed trace ID for correlating events across services (legacy, use Traceparent)
    /// </summary>
    public string? TraceId { get; init; }
    
    /// <summary>
    /// Parent span ID for distributed tracing hierarchy (legacy, use Traceparent)
    /// </summary>
    public string? SpanId { get; init; }
    
    /// <summary>
    /// Trace state for propagating vendor-specific context
    /// </summary>
    public Dictionary<string, string>? TraceState { get; init; }
    
    /// <summary>
    /// Correlation ID for linking related operations across services (business flow tracking)
    /// Remains constant throughout a business transaction (e.g., order creation flow)
    /// </summary>
    public string? CorrelationId { get; init; }
    
    /// <summary>
    /// Causation ID - ID of the event/command that directly caused this event
    /// Forms a chain: Command -> Event1 (CausationId = CommandId) -> Event2 (CausationId = Event1.Id)
    /// </summary>
    public Guid? CausationId { get; init; }
    
    /// <summary>
    /// Optional tenant ID for multi-tenant systems
    /// </summary>
    public string? TenantId { get; init; }
    
    /// <summary>
    /// Additional headers for extensibility (feature flags, routing hints, etc.)
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    protected IntegrationEvent()
    {
        EventType = GetType().Name;
    }

    protected IntegrationEvent(Guid aggregateId)
    {
        AggregateId = aggregateId;
        EventType = GetType().Name;
    }
    
    /// <summary>
    /// Create a new event with correlation/causation tracking
    /// </summary>
    protected IntegrationEvent(Guid aggregateId, string? correlationId, Guid? causationId, string producer)
    {
        AggregateId = aggregateId;
        EventType = GetType().Name;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        CausationId = causationId;
        Producer = producer;
    }
}
