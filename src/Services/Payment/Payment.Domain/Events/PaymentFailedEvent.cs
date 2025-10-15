using BuildingBlocks.Domain.Events;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment fails
/// </summary>
public sealed record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    string ErrorCode,
    DateTime FailedAt) : DomainEvent;
