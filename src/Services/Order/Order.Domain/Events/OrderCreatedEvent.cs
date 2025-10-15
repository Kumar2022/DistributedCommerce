using Order.Domain.ValueObjects;

namespace Order.Domain.Events;

/// <summary>
/// Event raised when a new order is created
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    CustomerId CustomerId,
    Address ShippingAddress,
    List<OrderItem> Items,
    Money TotalAmount,
    DateTime CreatedAt) : DomainEvent;
