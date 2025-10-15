using BuildingBlocks.EventBus.Abstractions;

namespace Payment.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a payment is created
/// </summary>
public sealed record PaymentCreatedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod) : IntegrationEvent(PaymentId);
