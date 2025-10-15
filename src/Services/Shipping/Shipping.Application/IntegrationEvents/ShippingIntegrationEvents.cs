using BuildingBlocks.EventBus.Abstractions;

namespace Shipping.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a shipment is created
/// </summary>
public sealed record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    DateTime CreatedAt
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is picked up
/// </summary>
public sealed record ShipmentPickedUpIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    DateTime PickupTime
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is in transit
/// </summary>
public sealed record ShipmentInTransitIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string CurrentLocation,
    DateTime EstimatedDelivery
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is out for delivery
/// </summary>
public sealed record ShipmentOutForDeliveryIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    DateTime EstimatedDelivery
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is delivered
/// </summary>
public sealed record ShipmentDeliveredIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    DateTime DeliveryTime,
    string RecipientName
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment delivery fails
/// </summary>
public sealed record ShipmentDeliveryFailedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Reason,
    int AttemptNumber,
    DateTime? NextAttemptTime
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is cancelled
/// </summary>
public sealed record ShipmentCancelledIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime CancelledAt
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when a shipment is returned
/// </summary>
public sealed record ShipmentReturnedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime ReturnedAt
) : IntegrationEvent(ShipmentId);

/// <summary>
/// Integration event published when shipment tracking is updated
/// </summary>
public sealed record ShipmentTrackingUpdatedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string Location,
    string Status,
    string Description,
    DateTime Timestamp
) : IntegrationEvent(ShipmentId);
