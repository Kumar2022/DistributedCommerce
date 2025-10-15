namespace BuildingBlocks.Domain.Events;

/// <summary>
/// Base class for domain events following event-driven architecture principles
/// </summary>
public abstract record DomainEvent(Guid EventId, DateTime OccurredAt, Guid? CorrelationId = null)
    : IDomainEvent
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow, null)
    {
    }
}
