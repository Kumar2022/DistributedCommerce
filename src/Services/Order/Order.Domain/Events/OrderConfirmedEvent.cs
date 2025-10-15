namespace Order.Domain.Events;

/// <summary>
/// Event raised when an order is confirmed
/// </summary>
public sealed record OrderConfirmedEvent(
    Guid OrderId,
    DateTime ConfirmedAt) : DomainEvent;
