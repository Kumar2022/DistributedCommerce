using BuildingBlocks.EventBus.Abstractions;

namespace Order.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a new order is created
/// Triggers inventory reservation in Inventory Service
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt) : IntegrationEvent(OrderId);

/// <summary>
/// Integration event published when order is confirmed (after payment succeeded)
/// Triggers shipment creation in Shipping Service
/// </summary>
public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    DateTime ConfirmedAt) : IntegrationEvent(OrderId);

/// <summary>
/// Integration event published when order is cancelled
/// Triggers inventory release in Inventory Service
/// </summary>
public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime CancelledAt) : IntegrationEvent(OrderId);

/// <summary>
/// Integration event published when order is shipped
/// Notifies customer via Notification Service
/// </summary>
public sealed record OrderShippedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string TrackingNumber,
    DateTime ShippedAt) : IntegrationEvent(OrderId);

/// <summary>
/// DTO for order item in integration events
/// </summary>
public sealed record OrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string ProductName);
