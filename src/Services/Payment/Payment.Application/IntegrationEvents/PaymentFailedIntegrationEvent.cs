using BuildingBlocks.EventBus.Abstractions;

namespace Payment.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a payment fails
/// </summary>
public sealed record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    string ErrorCode) : IntegrationEvent(PaymentId);
