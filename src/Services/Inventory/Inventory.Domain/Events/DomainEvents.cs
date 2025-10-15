namespace Inventory.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    string Sku,
    string Name,
    int InitialStock) : DomainEvent;

public record StockReservedEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int AvailableQuantity) : DomainEvent;

public record StockDeductedEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int RemainingStock,
    int AvailableQuantity) : DomainEvent;

public record StockReleasedEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int AvailableQuantity) : DomainEvent;

public record StockAdjustedEvent(
    Guid ProductId,
    int PreviousStock,
    int NewStock,
    int AdjustmentQuantity,
    string Reason) : DomainEvent;

public record LowStockDetectedEvent(
    Guid ProductId,
    string Sku,
    int CurrentStock,
    int ReorderLevel,
    int ReorderQuantity) : DomainEvent;

public record ReservationExpiredEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity) : DomainEvent;
