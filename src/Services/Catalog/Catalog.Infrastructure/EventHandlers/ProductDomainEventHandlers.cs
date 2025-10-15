using BuildingBlocks.EventBus.Abstractions;
using Catalog.Application.IntegrationEvents;
using Catalog.Domain.Events;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Infrastructure.EventHandlers;

/// <summary>
/// Handles ProductCreated domain event and publishes integration event to Kafka
/// </summary>
public class ProductCreatedDomainEventHandler(
    IEventBus eventBus,
    ICatalogProductRepository productRepository,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    public async Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ProductCreatedEvent for Product {ProductId}", notification.ProductId);

        try
        {
            // Fetch full product details from repository
            var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found for ProductCreatedEvent", notification.ProductId);
                return;
            }

            var integrationEvent = new ProductCreatedIntegrationEvent(
                product.Id,
                product.Name,
                product.Sku,
                product.CategoryId,
                product.Price,
                product.Currency
            );

            await eventBus.PublishAsync(integrationEvent, cancellationToken);

            logger.LogInformation("Published ProductCreatedIntegrationEvent for Product {ProductId} to Kafka topic", notification.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ProductCreatedEvent for Product {ProductId}", notification.ProductId);
            throw;
        }
    }
}

/// <summary>
/// Handles ProductPriceChanged domain event and publishes integration event to Kafka
/// </summary>
public class ProductPriceChangedDomainEventHandler(
    IEventBus eventBus,
    ICatalogProductRepository productRepository,
    ILogger<ProductPriceChangedDomainEventHandler> logger)
    : INotificationHandler<ProductPriceChangedEvent>
{
    public async Task Handle(ProductPriceChangedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ProductPriceChangedEvent for Product {ProductId}: {OldPrice} -> {NewPrice}", 
            notification.ProductId, notification.OldPrice, notification.NewPrice);

        try
        {
            // Fetch product to get currency
            var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found for ProductPriceChangedEvent", notification.ProductId);
                return;
            }

            var integrationEvent = new ProductPriceChangedIntegrationEvent(
                notification.ProductId,
                notification.OldPrice,
                notification.NewPrice,
                product.Currency
            );

            await eventBus.PublishAsync(integrationEvent, cancellationToken);

            logger.LogInformation("Published ProductPriceChangedIntegrationEvent for Product {ProductId} to Kafka topic", notification.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ProductPriceChangedEvent for Product {ProductId}", notification.ProductId);
            throw;
        }
    }
}

/// <summary>
/// Handles ProductPublished domain event and publishes integration event to Kafka
/// </summary>
public class ProductPublishedDomainEventHandler(
    IEventBus eventBus,
    ICatalogProductRepository productRepository,
    ILogger<ProductPublishedDomainEventHandler> logger)
    : INotificationHandler<ProductPublishedEvent>
{
    public async Task Handle(ProductPublishedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ProductPublishedEvent for Product {ProductId}", notification.ProductId);

        try
        {
            // Fetch product to get SKU and other details
            var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found for ProductPublishedEvent", notification.ProductId);
                return;
            }

            var integrationEvent = new ProductPublishedIntegrationEvent(
                product.Id,
                product.Name,
                product.Sku,
                DateTime.UtcNow
            );

            await eventBus.PublishAsync(integrationEvent, cancellationToken);

            logger.LogInformation("Published ProductPublishedIntegrationEvent for Product {ProductId} (SKU: {Sku}) to Kafka topic", 
                notification.ProductId, product.Sku);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ProductPublishedEvent for Product {ProductId}", notification.ProductId);
            throw;
        }
    }
}

/// <summary>
/// Handles ProductUnpublished domain event and publishes integration event to Kafka
/// </summary>
public class ProductUnpublishedDomainEventHandler(
    IEventBus eventBus,
    ICatalogProductRepository productRepository,
    ILogger<ProductUnpublishedDomainEventHandler> logger)
    : INotificationHandler<ProductUnpublishedEvent>
{
    public async Task Handle(ProductUnpublishedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ProductUnpublishedEvent for Product {ProductId}", notification.ProductId);

        try
        {
            // Fetch product to get SKU
            var product = await productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found for ProductUnpublishedEvent", notification.ProductId);
                return;
            }

            var integrationEvent = new ProductUnpublishedIntegrationEvent(
                product.Id,
                product.Sku
            );

            await eventBus.PublishAsync(integrationEvent, cancellationToken);

            logger.LogInformation("Published ProductUnpublishedIntegrationEvent for Product {ProductId} (SKU: {Sku}) to Kafka topic", 
                notification.ProductId, product.Sku);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ProductUnpublishedEvent for Product {ProductId}", notification.ProductId);
            throw;
        }
    }
}

