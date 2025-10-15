using MediatR;

namespace BuildingBlocks.Domain.Events;

/// <summary>
/// Marker interface for all domain events
/// Domain events represent something that happened in the domain that domain experts care about
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// When the event occurred (UTC)
    /// </summary>
    DateTime OccurredAt { get; }
    
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    Guid? CorrelationId { get; }
}
