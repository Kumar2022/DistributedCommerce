namespace BuildingBlocks.Domain.Aggregates;

/// <summary>
/// Marker interface for Aggregate Roots in Domain-Driven Design
/// Aggregate Root is the entry point for accessing the aggregate and enforces invariants
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier</typeparam>
public interface IAggregateRoot<out TId> where TId : notnull
{
    TId Id { get; }
    
    /// <summary>
    /// Version number for optimistic concurrency control
    /// </summary>
    long Version { get; }
}
