namespace Order.Domain.Events;

/// <summary>
/// Event raised when an order is shipped
/// </summary>
public sealed record OrderShippedEvent(
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    DateTime ShippedAt) : DomainEvent;
