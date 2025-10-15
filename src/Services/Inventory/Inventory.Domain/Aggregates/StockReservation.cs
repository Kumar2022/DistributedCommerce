namespace Inventory.Domain.Aggregates;

/// <summary>
/// Stock reservation entity - tracks time-bound stock reservations for orders
/// </summary>
public class StockReservation : Entity<Guid>
{
    public Guid ReservationId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private StockReservation() { } // EF Core

    public static StockReservation Create(
        Guid reservationId,
        Guid productId,
        Guid orderId,
        int quantity,
        DateTime expiresAt)
    {
        return new StockReservation
        {
            Id = reservationId,
            ReservationId = reservationId,
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Status = ReservationStatus.Active
        };
    }

    public bool IsExpired() => 
        Status == ReservationStatus.Active && DateTime.UtcNow > ExpiresAt;

    public void Confirm()
    {
        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        Status = ReservationStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = ReservationStatus.Expired;
        ReleasedAt = DateTime.UtcNow;
    }
}

public enum ReservationStatus
{
    Active = 0,
    Confirmed = 1,
    Released = 2,
    Expired = 3
}
