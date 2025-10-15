using Shipping.Domain.Events;
using Shipping.Domain.ValueObjects;

namespace Shipping.Domain.Aggregates;

/// <summary>
/// Shipment Aggregate Root
/// Represents a shipment with tracking, status, and delivery information
/// Follows FAANG-scale design with rich domain logic and event sourcing
/// </summary>
public class Shipment : AggregateRoot<Guid>
{
    private readonly List<TrackingInfo> _trackingHistory = new();

    public Guid OrderId { get; private set; }
    public string TrackingNumber { get; private set; }
    public Carrier Carrier { get; private set; }
    public string? CarrierTrackingUrl { get; private set; }
    
    public ShippingAddress ShippingAddress { get; private set; }
    public Package Package { get; private set; }
    
    public ShipmentStatus Status { get; private set; }
    public DeliverySpeed DeliverySpeed { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? PickupTime { get; private set; }
    public DateTime? EstimatedDelivery { get; private set; }
    public DateTime? ActualDelivery { get; private set; }
    
    // Delivery details
    public string? RecipientName { get; private set; }
    public string? SignatureUrl { get; private set; }
    public int DeliveryAttempts { get; private set; }
    public string? LastDeliveryFailureReason { get; private set; }
    
    // Costs
    public decimal ShippingCost { get; private set; }
    public string Currency { get; private set; }
    
    // Tracking history
    public IReadOnlyList<TrackingInfo> TrackingHistory => _trackingHistory.AsReadOnly();
    
    // Metadata
    public string? Notes { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Shipment() { } // EF Core

    private Shipment(
        Guid id,
        Guid orderId,
        string trackingNumber,
        Carrier carrier,
        ShippingAddress shippingAddress,
        Package package,
        DeliverySpeed deliverySpeed,
        decimal shippingCost,
        string currency = "USD")
    {
        Id = id;
        OrderId = orderId;
        TrackingNumber = trackingNumber;
        Carrier = carrier;
        ShippingAddress = shippingAddress;
        Package = package;
        DeliverySpeed = deliverySpeed;
        ShippingCost = shippingCost;
        Currency = currency;
        Status = ShipmentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        DeliveryAttempts = 0;

        AddDomainEvent(new ShipmentCreatedEvent(Id, OrderId, TrackingNumber, carrier.ToString()));
    }

    public static Shipment Create(
        Guid orderId,
        string trackingNumber,
        Carrier carrier,
        ShippingAddress shippingAddress,
        Package package,
        DeliverySpeed deliverySpeed,
        decimal shippingCost,
        string currency = "USD")
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required", nameof(orderId));
        
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number is required", nameof(trackingNumber));
        
        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));
        
        if (package == null)
            throw new ArgumentNullException(nameof(package));
        
        if (shippingCost < 0)
            throw new ArgumentException("Shipping cost must be non-negative", nameof(shippingCost));

        return new Shipment(Guid.NewGuid(), orderId, trackingNumber, carrier, shippingAddress, 
            package, deliverySpeed, shippingCost, currency);
    }

    public void SchedulePickup(DateTime pickupTime)
    {
        if (Status != ShipmentStatus.Pending)
            throw new InvalidOperationException($"Cannot schedule pickup for shipment in {Status} status");
        
        if (pickupTime <= DateTime.UtcNow)
            throw new ArgumentException("Pickup time must be in the future", nameof(pickupTime));

        PickupTime = pickupTime;
        Status = ShipmentStatus.PickupScheduled;
        UpdatedAt = DateTime.UtcNow;
        
        EstimatedDelivery = CalculateEstimatedDelivery(pickupTime);
    }

    public void MarkAsPickedUp(DateTime pickupTime)
    {
        if (Status != ShipmentStatus.Pending && Status != ShipmentStatus.PickupScheduled)
            throw new InvalidOperationException($"Cannot mark as picked up from {Status} status");

        PickupTime = pickupTime;
        Status = ShipmentStatus.PickedUp;
        UpdatedAt = DateTime.UtcNow;
        
        if (!EstimatedDelivery.HasValue)
            EstimatedDelivery = CalculateEstimatedDelivery(pickupTime);

        AddDomainEvent(new ShipmentPickedUpEvent(Id, TrackingNumber, Carrier.ToString(), pickupTime));
    }

    public void UpdateToInTransit(string location, DateTime? estimatedDelivery = null)
    {
        if (Status != ShipmentStatus.PickedUp && Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException($"Cannot update to in transit from {Status} status");

        var oldStatus = Status.ToString();
        Status = ShipmentStatus.InTransit;
        UpdatedAt = DateTime.UtcNow;
        
        if (estimatedDelivery.HasValue)
            EstimatedDelivery = estimatedDelivery;

        AddDomainEvent(new ShipmentInTransitEvent(Id, TrackingNumber, location, EstimatedDelivery ?? DateTime.UtcNow.AddDays(3)));
        AddDomainEvent(new ShipmentStatusChangedEvent(Id, oldStatus, Status.ToString(), location, DateTime.UtcNow));
    }

    public void MarkAsOutForDelivery(DateTime? estimatedDelivery = null)
    {
        if (Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException($"Cannot mark as out for delivery from {Status} status");

        var oldStatus = Status.ToString();
        Status = ShipmentStatus.OutForDelivery;
        UpdatedAt = DateTime.UtcNow;
        
        if (estimatedDelivery.HasValue)
            EstimatedDelivery = estimatedDelivery;

        AddDomainEvent(new ShipmentOutForDeliveryEvent(Id, TrackingNumber, EstimatedDelivery ?? DateTime.UtcNow));
        AddDomainEvent(new ShipmentStatusChangedEvent(Id, oldStatus, Status.ToString(), "Out for Delivery", DateTime.UtcNow));
    }

    public void MarkAsDelivered(DateTime deliveryTime, string recipientName, string? signatureUrl = null)
    {
        if (Status != ShipmentStatus.OutForDelivery && Status != ShipmentStatus.InTransit)
            throw new InvalidOperationException($"Cannot mark as delivered from {Status} status");
        
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("Recipient name is required", nameof(recipientName));

        var oldStatus = Status.ToString();
        Status = ShipmentStatus.Delivered;
        ActualDelivery = deliveryTime;
        RecipientName = recipientName;
        SignatureUrl = signatureUrl;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentDeliveredEvent(Id, OrderId, TrackingNumber, deliveryTime, recipientName, signatureUrl ?? string.Empty));
        AddDomainEvent(new ShipmentStatusChangedEvent(Id, oldStatus, Status.ToString(), "Delivered", DateTime.UtcNow));
    }

    public void MarkDeliveryFailed(string reason, DateTime? nextAttemptTime = null)
    {
        if (Status != ShipmentStatus.OutForDelivery)
            throw new InvalidOperationException($"Cannot mark delivery failed from {Status} status");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason is required", nameof(reason));

        DeliveryAttempts++;
        LastDeliveryFailureReason = reason;
        
        // After 3 failed attempts, mark as delivery failed permanently
        if (DeliveryAttempts >= 3)
        {
            Status = ShipmentStatus.DeliveryFailed;
        }
        
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentDeliveryFailedEvent(Id, TrackingNumber, reason, DeliveryAttempts, nextAttemptTime));
    }

    public void Cancel(string reason)
    {
        if (Status == ShipmentStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered shipment");
        
        if (Status == ShipmentStatus.Cancelled)
            throw new InvalidOperationException("Shipment is already cancelled");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        Status = ShipmentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentCancelledEvent(Id, OrderId, reason, DateTime.UtcNow));
    }

    public void MarkAsReturned(string reason)
    {
        if (Status != ShipmentStatus.Delivered && Status != ShipmentStatus.DeliveryFailed)
            throw new InvalidOperationException($"Cannot mark as returned from {Status} status");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Return reason is required", nameof(reason));

        Status = ShipmentStatus.Returned;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentReturnedEvent(Id, OrderId, reason, DateTime.UtcNow));
    }

    public void AddTrackingUpdate(string location, string status, string description, DateTime timestamp, string? coordinates = null)
    {
        var trackingInfo = new TrackingInfo(location, status, description, timestamp, coordinates);
        _trackingHistory.Add(trackingInfo);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentTrackingUpdatedEvent(Id, location, status, description, timestamp));
    }

    public void UpdateCarrierTrackingUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Tracking URL is required", nameof(url));

        CarrierTrackingUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddNotes(string notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    private DateTime CalculateEstimatedDelivery(DateTime fromDate)
    {
        return DeliverySpeed switch
        {
            DeliverySpeed.SameDay => fromDate.Date.AddHours(18), // Same day by 6 PM
            DeliverySpeed.Overnight => fromDate.AddDays(1),
            DeliverySpeed.Express => fromDate.AddDays(2),
            DeliverySpeed.Standard => fromDate.AddDays(5),
            _ => fromDate.AddDays(5)
        };
    }

    /// <summary>
    /// Check if delivery is delayed based on estimated delivery
    /// </summary>
    public bool IsDelayed()
    {
        if (!EstimatedDelivery.HasValue || Status == ShipmentStatus.Delivered)
            return false;
        
        return DateTime.UtcNow > EstimatedDelivery.Value && Status != ShipmentStatus.Delivered;
    }

    /// <summary>
    /// Get the current delay in hours (if delayed)
    /// </summary>
    public int? GetDelayHours()
    {
        if (!IsDelayed() || !EstimatedDelivery.HasValue)
            return null;
        
        return (int)(DateTime.UtcNow - EstimatedDelivery.Value).TotalHours;
    }
}
