using BuildingBlocks.EventBus.Abstractions;

namespace Inventory.Application.IntegrationEvents;

public record StockReservedIntegrationEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int AvailableQuantity,
    DateTime OccurredAt) : IntegrationEvent;

public record StockDeductedIntegrationEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int RemainingStock,
    DateTime OccurredAt) : IntegrationEvent;

public record StockReleasedIntegrationEvent(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int AvailableQuantity,
    DateTime OccurredAt) : IntegrationEvent;

public record LowStockAlertIntegrationEvent(
    Guid ProductId,
    string Sku,
    int CurrentStock,
    int ReorderLevel,
    int ReorderQuantity,
    DateTime OccurredAt) : IntegrationEvent;

public record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Sku,
    string Name,
    int InitialStock,
    DateTime OccurredAt) : IntegrationEvent;
