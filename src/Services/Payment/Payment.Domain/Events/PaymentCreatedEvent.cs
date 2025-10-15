using BuildingBlocks.Domain.Events;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment is created
/// </summary>
public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    DateTime CreatedAt) : DomainEvent;
