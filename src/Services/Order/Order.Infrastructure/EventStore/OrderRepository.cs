using Marten;
using Order.Application.DTOs;
using Order.Domain.Aggregates.OrderAggregate;

namespace Order.Infrastructure.EventStore;

/// <summary>
/// Order repository implementation using Marten event store
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly IDocumentSession _session;

    public OrderRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Domain.Aggregates.OrderAggregate.Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Marten will automatically rebuild the aggregate from events
        return await _session.Events.AggregateStreamAsync<Domain.Aggregates.OrderAggregate.Order>(
            id,
            token: cancellationToken);
    }

    public async Task AddAsync(
        Domain.Aggregates.OrderAggregate.Order order,
        CancellationToken cancellationToken = default)
    {
        // Store domain events to event stream
        _session.Events.StartStream<Domain.Aggregates.OrderAggregate.Order>(
            order.Id,
            order.DomainEvents.ToArray());

        order.ClearDomainEvents();

        await _session.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Domain.Aggregates.OrderAggregate.Order order,
        CancellationToken cancellationToken = default)
    {
        // Append new events to existing stream
        if (order.DomainEvents.Any())
        {
            _session.Events.Append(
                order.Id,
                order.DomainEvents.ToArray());

            order.ClearDomainEvents();
        }

        await _session.SaveChangesAsync(cancellationToken);
    }
}
