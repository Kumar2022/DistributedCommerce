using BuildingBlocks.Domain.Events;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment is refunded
/// </summary>
public sealed record PaymentRefundedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal RefundAmount,
    string Currency,
    string Reason,
    DateTime RefundedAt) : DomainEvent;
