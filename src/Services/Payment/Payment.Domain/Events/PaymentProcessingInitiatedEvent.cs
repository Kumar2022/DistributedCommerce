using BuildingBlocks.Domain.Events;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when payment processing is initiated
/// </summary>
public sealed record PaymentProcessingInitiatedEvent(
    Guid PaymentId,
    string ExternalPaymentId,
    DateTime InitiatedAt) : DomainEvent;
