using BuildingBlocks.Domain.Events;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment succeeds
/// </summary>
public sealed record PaymentSucceededEvent(
    Guid PaymentId,
    Guid OrderId,
    string ExternalPaymentId,
    decimal Amount,
    string Currency,
    DateTime ProcessedAt) : DomainEvent;
