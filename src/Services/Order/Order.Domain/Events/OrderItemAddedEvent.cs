using Order.Domain.ValueObjects;

namespace Order.Domain.Events;

/// <summary>
/// Event raised when an item is added to an order
/// </summary>
public sealed record OrderItemAddedEvent(
    Guid OrderId,
    OrderItem Item,
    DateTime AddedAt) : DomainEvent;
