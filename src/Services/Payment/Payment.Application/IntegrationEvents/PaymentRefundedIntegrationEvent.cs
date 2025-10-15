using BuildingBlocks.EventBus.Abstractions;

namespace Payment.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a payment is refunded
/// </summary>
public sealed record PaymentRefundedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal RefundAmount,
    string Currency,
    string Reason) : IntegrationEvent(PaymentId);
