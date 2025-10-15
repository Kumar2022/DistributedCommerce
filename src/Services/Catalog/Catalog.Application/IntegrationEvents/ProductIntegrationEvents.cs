using BuildingBlocks.EventBus.Abstractions;

namespace Catalog.Application.IntegrationEvents;

/// <summary>
/// Published when a product is created in the catalog
/// </summary>
public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Sku,
    Guid CategoryId,
    decimal Price,
    string Currency) : IntegrationEvent(ProductId);

/// <summary>
/// Published when a product is updated
/// </summary>
public sealed record ProductUpdatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Description,
    string Brand) : IntegrationEvent(ProductId);

/// <summary>
/// Published when product price changes
/// </summary>
public sealed record ProductPriceChangedIntegrationEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency) : IntegrationEvent(ProductId);

/// <summary>
/// Published when a product is published (made available for sale)
/// </summary>
public sealed record ProductPublishedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Sku,
    DateTime PublishedAt) : IntegrationEvent(ProductId);

/// <summary>
/// Published when a product is unpublished (removed from sale)
/// </summary>
public sealed record ProductUnpublishedIntegrationEvent(
    Guid ProductId,
    string Sku) : IntegrationEvent(ProductId);

/// <summary>
/// Published when inventory quantity is updated (synced from Inventory Service)
/// </summary>
public sealed record ProductInventoryUpdatedIntegrationEvent(
    Guid ProductId,
    string Sku,
    int AvailableQuantity) : IntegrationEvent(ProductId);
