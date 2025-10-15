using Order.Domain.Aggregates.OrderAggregate;

namespace Order.Application.DTOs;

/// <summary>
/// Repository interface for Order aggregate with event sourcing support
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Get order by ID (reconstructs from events)
    /// </summary>
    Task<Domain.Aggregates.OrderAggregate.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new order (stores events)
    /// </summary>
    Task AddAsync(Domain.Aggregates.OrderAggregate.Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update order (appends new events)
    /// </summary>
    Task UpdateAsync(Domain.Aggregates.OrderAggregate.Order order, CancellationToken cancellationToken = default);
}
