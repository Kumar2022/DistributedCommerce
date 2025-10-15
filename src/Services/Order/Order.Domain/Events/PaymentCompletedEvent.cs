namespace Order.Domain.Events;

/// <summary>
/// Event raised when payment is completed for an order
/// </summary>
public sealed record PaymentCompletedEvent(
    Guid OrderId,
    Guid PaymentId,
    DateTime CompletedAt) : DomainEvent;
