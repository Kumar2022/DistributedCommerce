namespace Notification.Application.IntegrationEvents;

// ========== Notification Integration Events (Published by this service) ==========

public record NotificationCreatedIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    string Subject,
    DateTime CreatedAt
) : IntegrationEvent;

public record NotificationSentIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    DateTime SentAt,
    string? ExternalId
) : IntegrationEvent;

public record NotificationDeliveredIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    DateTime DeliveredAt,
    string? ExternalId
) : IntegrationEvent;

public record NotificationFailedIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    string ErrorMessage,
    DateTime FailedAt,
    int RetryCount
) : IntegrationEvent;

// ========== Integration Events (Consumed from other services) ==========

// Order events
public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt
) : IntegrationEvent;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTime ConfirmedAt
) : IntegrationEvent;

public record OrderShippedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string TrackingNumber,
    DateTime ShippedAt
) : IntegrationEvent;

public record OrderDeliveredIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    DateTime DeliveredAt
) : IntegrationEvent;

public record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime CancelledAt
) : IntegrationEvent;

// Payment events
public record PaymentCompletedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string? TransactionId,
    string? PaymentMethod,
    DateTime CompletedAt
) : IntegrationEvent;

public record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime FailedAt
) : IntegrationEvent;

// Shipping events  
public record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    DateTime CreatedAt
) : IntegrationEvent;

public record ShipmentDeliveredIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string RecipientName,
    DateTime DeliveredAt
) : IntegrationEvent;
