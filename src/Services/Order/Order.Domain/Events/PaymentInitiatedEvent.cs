using Order.Domain.ValueObjects;

namespace Order.Domain.Events;

/// <summary>
/// Event raised when payment is initiated for an order
/// </summary>
public sealed record PaymentInitiatedEvent(
    Guid OrderId,
    Money Amount,
    string PaymentMethod,
    DateTime InitiatedAt) : DomainEvent;
