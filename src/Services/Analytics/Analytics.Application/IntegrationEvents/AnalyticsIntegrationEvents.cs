using BuildingBlocks.EventBus.Abstractions;

namespace Analytics.Application.IntegrationEvents;

// Events consumed by Analytics service to track metrics

public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime CreatedAt
) : IntegrationEvent;

public record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    DateTime ConfirmedAt
) : IntegrationEvent;

public record OrderCancelledIntegrationEvent(
    Guid OrderId,
    DateTime CancelledAt
) : IntegrationEvent;

public record OrderCompletedIntegrationEvent(
    Guid OrderId,
    DateTime CompletedAt
) : IntegrationEvent;

public record PaymentCompletedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    DateTime CompletedAt
) : IntegrationEvent;

public record PaymentRefundedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    DateTime RefundedAt
) : IntegrationEvent;

public record ProductViewedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    Guid? CustomerId,
    DateTime ViewedAt
) : IntegrationEvent;

public record ProductAddedToCartIntegrationEvent(
    Guid ProductId,
    string ProductName,
    Guid CustomerId,
    DateTime AddedAt
) : IntegrationEvent;

public record ProductPurchasedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    Guid CustomerId,
    decimal Price,
    int Quantity,
    DateTime PurchasedAt
) : IntegrationEvent;

public record StockAdjustedIntegrationEvent(
    Guid ProductId,
    int NewQuantity,
    DateTime AdjustedAt
) : IntegrationEvent;
