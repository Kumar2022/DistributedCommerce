namespace Shipping.Domain.Events;

/// <summary>
/// Domain event raised when a shipment is created
/// </summary>
public sealed record ShipmentCreatedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;

    public ShipmentCreatedEvent(Guid shipmentId, Guid orderId, string trackingNumber, string carrier)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Carrier = carrier;
    }
}

/// <summary>
/// Domain event raised when a shipment status changes
/// </summary>
public sealed record ShipmentStatusChangedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }

    public ShipmentStatusChangedEvent(Guid shipmentId, string oldStatus, string newStatus, string location, DateTime timestamp)
    {
        ShipmentId = shipmentId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Location = location;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Domain event raised when a shipment is picked up
/// </summary>
public sealed record ShipmentPickedUpEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public DateTime PickupTime { get; init; }

    public ShipmentPickedUpEvent(Guid shipmentId, string trackingNumber, string carrier, DateTime pickupTime)
    {
        ShipmentId = shipmentId;
        TrackingNumber = trackingNumber;
        Carrier = carrier;
        PickupTime = pickupTime;
    }
}

/// <summary>
/// Domain event raised when a shipment is in transit
/// </summary>
public sealed record ShipmentInTransitEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string CurrentLocation { get; init; } = string.Empty;
    public DateTime EstimatedDelivery { get; init; }

    public ShipmentInTransitEvent(Guid shipmentId, string trackingNumber, string currentLocation, DateTime estimatedDelivery)
    {
        ShipmentId = shipmentId;
        TrackingNumber = trackingNumber;
        CurrentLocation = currentLocation;
        EstimatedDelivery = estimatedDelivery;
    }
}

/// <summary>
/// Domain event raised when a shipment is out for delivery
/// </summary>
public sealed record ShipmentOutForDeliveryEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime EstimatedDelivery { get; init; }

    public ShipmentOutForDeliveryEvent(Guid shipmentId, string trackingNumber, DateTime estimatedDelivery)
    {
        ShipmentId = shipmentId;
        TrackingNumber = trackingNumber;
        EstimatedDelivery = estimatedDelivery;
    }
}

/// <summary>
/// Domain event raised when a shipment is delivered
/// </summary>
public sealed record ShipmentDeliveredEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime DeliveryTime { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public string SignatureUrl { get; init; } = string.Empty;

    public ShipmentDeliveredEvent(Guid shipmentId, Guid orderId, string trackingNumber, DateTime deliveryTime, string recipientName, string signatureUrl)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        DeliveryTime = deliveryTime;
        RecipientName = recipientName;
        SignatureUrl = signatureUrl;
    }
}

/// <summary>
/// Domain event raised when a shipment fails delivery
/// </summary>
public sealed record ShipmentDeliveryFailedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public DateTime? NextAttemptTime { get; init; }

    public ShipmentDeliveryFailedEvent(Guid shipmentId, string trackingNumber, string reason, int attemptNumber, DateTime? nextAttemptTime)
    {
        ShipmentId = shipmentId;
        TrackingNumber = trackingNumber;
        Reason = reason;
        AttemptNumber = attemptNumber;
        NextAttemptTime = nextAttemptTime;
    }
}

/// <summary>
/// Domain event raised when a shipment is cancelled
/// </summary>
public sealed record ShipmentCancelledEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }

    public ShipmentCancelledEvent(Guid shipmentId, Guid orderId, string reason, DateTime cancelledAt)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        Reason = reason;
        CancelledAt = cancelledAt;
    }
}

/// <summary>
/// Domain event raised when a shipment is returned
/// </summary>
public sealed record ShipmentReturnedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime ReturnedAt { get; init; }

    public ShipmentReturnedEvent(Guid shipmentId, Guid orderId, string reason, DateTime returnedAt)
    {
        ShipmentId = shipmentId;
        OrderId = orderId;
        Reason = reason;
        ReturnedAt = returnedAt;
    }
}

/// <summary>
/// Domain event raised when shipment tracking is updated
/// </summary>
public sealed record ShipmentTrackingUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; init; }
    public Guid ShipmentId { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }

    public ShipmentTrackingUpdatedEvent(Guid shipmentId, string location, string status, string description, DateTime timestamp)
    {
        ShipmentId = shipmentId;
        Location = location;
        Status = status;
        Description = description;
        Timestamp = timestamp;
    }
}
