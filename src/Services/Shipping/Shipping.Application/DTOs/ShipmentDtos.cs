namespace Shipping.Application.DTOs;

public record ShipmentDto(
    Guid Id,
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    string Status,
    ShippingAddressDto ShippingAddress,
    PackageDto Package,
    string DeliverySpeed,
    decimal ShippingCost,
    string Currency,
    DateTime CreatedAt,
    DateTime? PickupTime,
    DateTime? EstimatedDelivery,
    DateTime? ActualDelivery,
    string? RecipientName,
    int DeliveryAttempts,
    List<TrackingInfoDto> TrackingHistory,
    bool IsDelayed,
    int? DelayHours
);

public record ShippingAddressDto(
    string RecipientName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country,
    string? Email
);

public record PackageDto(
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    string WeightUnit,
    string DimensionUnit
);

public record TrackingInfoDto(
    string Location,
    string Status,
    string Description,
    DateTime Timestamp,
    string? Coordinates
);

public record CreateShipmentDto(
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    ShippingAddressDto ShippingAddress,
    PackageDto Package,
    string DeliverySpeed,
    decimal ShippingCost,
    string Currency = "USD"
);
