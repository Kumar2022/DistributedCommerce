using BuildingBlocks.EventBus.Abstractions;

namespace Payment.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a payment succeeds
/// </summary>
public sealed record PaymentSucceededIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    string ExternalPaymentId,
    decimal Amount,
    string Currency) : IntegrationEvent(PaymentId);
