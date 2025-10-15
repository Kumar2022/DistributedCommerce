namespace Order.Domain.Events;

/// <summary>
/// Event raised when an order is cancelled
/// </summary>
public sealed record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt) : DomainEvent;
