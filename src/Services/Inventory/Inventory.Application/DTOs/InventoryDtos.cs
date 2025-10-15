namespace Inventory.Application.DTOs;

public record ProductDto(
    Guid ProductId,
    string Sku,
    string Name,
    int StockQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int ReorderLevel,
    int ReorderQuantity,
    DateTime LastRestockDate,
    DateTime CreatedAt);

public record StockReservationDto(
    Guid ReservationId,
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    DateTime ReservedAt,
    DateTime ExpiresAt,
    string Status,
    DateTime? ConfirmedAt,
    DateTime? ReleasedAt);
