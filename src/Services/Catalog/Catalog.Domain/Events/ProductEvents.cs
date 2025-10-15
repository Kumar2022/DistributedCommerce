namespace Catalog.Domain.Events;

// Domain Events
public record ProductCreatedEvent(Guid ProductId, string Name, string Sku, Guid CategoryId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}

public record ProductUpdatedEvent(Guid ProductId, string Name, string Description) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}

public record ProductPriceChangedEvent(Guid ProductId, decimal OldPrice, decimal NewPrice) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}

public record ProductPublishedEvent(Guid ProductId, string Name) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}

public record ProductUnpublishedEvent(Guid ProductId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}

public record ProductInventoryUpdatedEvent(Guid ProductId, int OldQuantity, int NewQuantity) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
}
