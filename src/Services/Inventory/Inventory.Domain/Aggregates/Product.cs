namespace Inventory.Domain.Aggregates;

/// <summary>
/// Product aggregate - manages stock levels and reservations with optimistic concurrency
/// </summary>
public class Product : AggregateRoot<Guid>
{
    private readonly List<StockReservation> _reservations = new();

    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => StockQuantity - ReservedQuantity;
    public int ReorderLevel { get; private set; }
    public int ReorderQuantity { get; private set; }
    public DateTime LastRestockDate { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>(); // For optimistic concurrency
    public IReadOnlyCollection<StockReservation> Reservations => _reservations.AsReadOnly();

    private Product() { } // EF Core

    public static Result<Product> Create(
        string sku,
        string name,
        int initialStock,
        int reorderLevel = 10,
        int reorderQuantity = 100)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Result.Failure<Product>(Error.Validation("Product", "SKU cannot be empty"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(Error.Validation("Product", "Product name cannot be empty"));

        if (initialStock < 0)
            return Result.Failure<Product>(Error.Validation("Product", "Initial stock cannot be negative"));

        if (reorderLevel < 0)
            return Result.Failure<Product>(Error.Validation("Product", "Reorder level cannot be negative"));

        if (reorderQuantity <= 0)
            return Result.Failure<Product>(Error.Validation("Product", "Reorder quantity must be positive"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            StockQuantity = initialStock,
            ReservedQuantity = 0,
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderQuantity,
            LastRestockDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new Events.ProductCreatedEvent(
            product.ProductId,
            product.Sku,
            product.Name,
            product.StockQuantity));

        return Result<Product>.Success(product);
    }

    /// <summary>
    /// Reserve stock for an order (optimistic concurrency)
    /// </summary>
    public Result ReserveStock(Guid orderId, int quantity, TimeSpan? expirationTime = null)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("StockOperation", "Quantity must be positive"));

        if (AvailableQuantity < quantity)
            return Result.Failure(Error.Validation("StockOperation", $"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}"));

        var expiration = expirationTime ?? TimeSpan.FromMinutes(15); // Default 15 min reservation
        var reservation = StockReservation.Create(
            Guid.NewGuid(),
            ProductId,
            orderId,
            quantity,
            DateTime.UtcNow.Add(expiration));

        _reservations.Add(reservation);
        ReservedQuantity += quantity;

        AddDomainEvent(new Events.StockReservedEvent(
            ProductId,
            orderId,
            quantity,
            AvailableQuantity));

        // Check if reorder needed
        if (AvailableQuantity <= ReorderLevel)
        {
            AddDomainEvent(new Events.LowStockDetectedEvent(
                ProductId,
                Sku,
                AvailableQuantity,
                ReorderLevel,
                ReorderQuantity));
        }

        return Result.Success();
    }

    /// <summary>
    /// Confirm reservation and deduct stock (when order is confirmed)
    /// </summary>
    public Result ConfirmReservation(Guid orderId)
    {
        var reservation = _reservations.FirstOrDefault(r => 
            r.OrderId == orderId && r.Status == ReservationStatus.Active);

        if (reservation == null)
            return Result.Failure(Error.Validation("StockOperation", $"No active reservation found for order {orderId}"));

        reservation.Confirm();
        StockQuantity -= reservation.Quantity;
        ReservedQuantity -= reservation.Quantity;

        AddDomainEvent(new Events.StockDeductedEvent(
            ProductId,
            orderId,
            reservation.Quantity,
            StockQuantity,
            AvailableQuantity));

        return Result.Success();
    }

    /// <summary>
    /// Release reservation (when order is cancelled or expires)
    /// </summary>
    public Result ReleaseReservation(Guid orderId)
    {
        var reservation = _reservations.FirstOrDefault(r => 
            r.OrderId == orderId && r.Status == ReservationStatus.Active);

        if (reservation == null)
            return Result.Failure(Error.Validation("StockOperation", $"No active reservation found for order {orderId}"));

        reservation.Release();
        ReservedQuantity -= reservation.Quantity;

        AddDomainEvent(new Events.StockReleasedEvent(
            ProductId,
            orderId,
            reservation.Quantity,
            AvailableQuantity));

        return Result.Success();
    }

    /// <summary>
    /// Manually adjust stock (restock, damage, etc.)
    /// </summary>
    public Result AdjustStock(int quantity, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("StockOperation", "Adjustment reason is required"));

        var previousStock = StockQuantity;
        StockQuantity += quantity;

        if (StockQuantity < 0)
        {
            StockQuantity = previousStock;
            return Result.Failure(Error.Validation("StockOperation", "Stock cannot be negative"));
        }

        if (quantity > 0)
        {
            LastRestockDate = DateTime.UtcNow;
        }

        AddDomainEvent(new Events.StockAdjustedEvent(
            ProductId,
            previousStock,
            StockQuantity,
            quantity,
            reason));

        return Result.Success();
    }

    /// <summary>
    /// Release expired reservations
    /// </summary>
    public Result ReleaseExpiredReservations()
    {
        var expiredReservations = _reservations
            .Where(r => r.Status == ReservationStatus.Active && r.IsExpired())
            .ToList();

        if (!expiredReservations.Any())
            return Result.Success();

        foreach (var reservation in expiredReservations)
        {
            reservation.Expire();
            ReservedQuantity -= reservation.Quantity;

            AddDomainEvent(new Events.ReservationExpiredEvent(
                ProductId,
                reservation.OrderId,
                reservation.Quantity));
        }

        return Result.Success();
    }

    /// <summary>
    /// Update reorder settings
    /// </summary>
    public Result UpdateReorderSettings(int reorderLevel, int reorderQuantity)
    {
        if (reorderLevel < 0)
            return Result.Failure(Error.Validation("StockOperation", "Reorder level cannot be negative"));

        if (reorderQuantity <= 0)
            return Result.Failure(Error.Validation("StockOperation", "Reorder quantity must be positive"));

        ReorderLevel = reorderLevel;
        ReorderQuantity = reorderQuantity;

        return Result.Success();
    }
}
