namespace Shipping.Domain.ValueObjects;

/// <summary>
/// Shipping address value object
/// </summary>
public sealed record ShippingAddress
{
    public string RecipientName { get; init; }
    public string Phone { get; init; }
    public string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string City { get; init; }
    public string StateOrProvince { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }
    public string? Email { get; init; }

    private ShippingAddress() { }

    public ShippingAddress(
        string recipientName,
        string phone,
        string addressLine1,
        string city,
        string stateOrProvince,
        string postalCode,
        string country,
        string? addressLine2 = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("Recipient name is required", nameof(recipientName));
        
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 is required", nameof(addressLine1));
        
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));
        
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required", nameof(postalCode));
        
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        RecipientName = recipientName;
        Phone = phone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateOrProvince = stateOrProvince;
        PostalCode = postalCode;
        Country = country;
        Email = email;
    }

    public string GetFullAddress()
    {
        var address = AddressLine1;
        if (!string.IsNullOrWhiteSpace(AddressLine2))
            address += $", {AddressLine2}";
        address += $", {City}, {StateOrProvince} {PostalCode}, {Country}";
        return address;
    }
}

/// <summary>
/// Package dimensions and weight
/// </summary>
public sealed record Package
{
    public decimal Weight { get; init; } // in kg
    public decimal Length { get; init; } // in cm
    public decimal Width { get; init; } // in cm
    public decimal Height { get; init; } // in cm
    public string WeightUnit { get; init; } = "kg";
    public string DimensionUnit { get; init; } = "cm";

    private Package() { }

    public Package(decimal weight, decimal length, decimal width, decimal height)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be positive", nameof(weight));
        
        if (length <= 0 || width <= 0 || height <= 0)
            throw new ArgumentException("Dimensions must be positive");

        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
    }

    public decimal GetVolume() => Length * Width * Height;
    
    public decimal GetVolumetricWeight() => GetVolume() / 5000; // Standard divisor
}

/// <summary>
/// Tracking information value object
/// </summary>
public sealed record TrackingInfo
{
    public string Location { get; init; }
    public string Status { get; init; }
    public string Description { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Coordinates { get; init; }

    private TrackingInfo() { }

    public TrackingInfo(string location, string status, string description, DateTime timestamp, string? coordinates = null)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required", nameof(location));
        
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));

        Location = location;
        Status = status;
        Description = description;
        Timestamp = timestamp;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Shipment status enumeration
/// </summary>
public enum ShipmentStatus
{
    Pending,
    PickupScheduled,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    DeliveryFailed,
    Cancelled,
    Returned
}

/// <summary>
/// Carrier enumeration
/// </summary>
public enum Carrier
{
    FedEx,
    UPS,
    USPS,
    DHL,
    Custom
}

/// <summary>
/// Delivery speed enumeration
/// </summary>
public enum DeliverySpeed
{
    Standard,    // 5-7 days
    Express,     // 2-3 days
    Overnight,   // 1 day
    SameDay      // Same day
}
