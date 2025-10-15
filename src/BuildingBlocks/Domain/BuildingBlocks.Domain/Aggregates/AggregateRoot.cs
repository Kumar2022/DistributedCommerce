namespace BuildingBlocks.Domain.Aggregates;

/// <summary>
/// Base class for Aggregate Roots with event sourcing support
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId> 
    where TId : notnull
{
    /// <summary>
    /// Version for optimistic concurrency control
    /// Incremented with each state change
    /// </summary>
    public long Version { get; protected set; }

    /// <summary>
    /// Apply and record a domain event
    /// </summary>
    /// <param name="domainEvent">The event to apply</param>
    protected void ApplyEvent(IDomainEvent domainEvent)
    {
        // Apply the event to update state
        ((dynamic)this).When((dynamic)domainEvent);
        
        // Add to pending events for publishing
        AddDomainEvent(domainEvent);
        
        // Increment version
        Version++;
    }

    /// <summary>
    /// Apply an event during rehydration (from event store)
    /// Does not add to pending events or increment version
    /// </summary>
    /// <param name="domainEvent">The event to apply</param>
    protected void ApplyEventFromHistory(IDomainEvent domainEvent)
    {
        ((dynamic)this).When((dynamic)domainEvent);
        Version++;
    }
}
