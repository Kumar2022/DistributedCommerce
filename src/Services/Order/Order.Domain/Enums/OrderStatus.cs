namespace Order.Domain.Enums;

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    PaymentInitiated = 2,
    PaymentCompleted = 3,
    Confirmed = 4,
    Shipped = 5,
    Delivered = 6,
    Cancelled = 7,
    Failed = 8
}
