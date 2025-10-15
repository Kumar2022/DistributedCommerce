using Inventory.Application.IntegrationEvents;
using Inventory.Domain.Events;
using MediatR;

namespace Inventory.Infrastructure.DomainEventHandlers;

public class StockReservedDomainEventHandler : INotificationHandler<StockReservedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<StockReservedDomainEventHandler> _logger;

    public StockReservedDomainEventHandler(
        IEventBus eventBus,
        ILogger<StockReservedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(StockReservedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing StockReservedIntegrationEvent for Product={ProductId}, Order={OrderId}",
            notification.ProductId, notification.OrderId);

        var integrationEvent = new StockReservedIntegrationEvent(
            notification.ProductId,
            notification.OrderId,
            notification.Quantity,
            notification.AvailableQuantity,
            notification.OccurredAt);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}

public class StockDeductedDomainEventHandler : INotificationHandler<StockDeductedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<StockDeductedDomainEventHandler> _logger;

    public StockDeductedDomainEventHandler(
        IEventBus eventBus,
        ILogger<StockDeductedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(StockDeductedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing StockDeductedIntegrationEvent for Product={ProductId}, Order={OrderId}",
            notification.ProductId, notification.OrderId);

        var integrationEvent = new StockDeductedIntegrationEvent(
            notification.ProductId,
            notification.OrderId,
            notification.Quantity,
            notification.RemainingStock,
            notification.OccurredAt);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}

public class StockReleasedDomainEventHandler : INotificationHandler<StockReleasedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<StockReleasedDomainEventHandler> _logger;

    public StockReleasedDomainEventHandler(
        IEventBus eventBus,
        ILogger<StockReleasedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(StockReleasedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing StockReleasedIntegrationEvent for Product={ProductId}, Order={OrderId}",
            notification.ProductId, notification.OrderId);

        var integrationEvent = new StockReleasedIntegrationEvent(
            notification.ProductId,
            notification.OrderId,
            notification.Quantity,
            notification.AvailableQuantity,
            notification.OccurredAt);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}

public class LowStockDetectedDomainEventHandler : INotificationHandler<LowStockDetectedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<LowStockDetectedDomainEventHandler> _logger;

    public LowStockDetectedDomainEventHandler(
        IEventBus eventBus,
        ILogger<LowStockDetectedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(LowStockDetectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Low stock detected for Product={ProductId}, SKU={Sku}, Available={CurrentStock}, ReorderLevel={ReorderLevel}",
            notification.ProductId, notification.Sku, notification.CurrentStock, notification.ReorderLevel);

        var integrationEvent = new LowStockAlertIntegrationEvent(
            notification.ProductId,
            notification.Sku,
            notification.CurrentStock,
            notification.ReorderLevel,
            notification.ReorderQuantity,
            notification.OccurredAt);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}

public class ProductCreatedDomainEventHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProductCreatedDomainEventHandler> _logger;

    public ProductCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<ProductCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing ProductCreatedIntegrationEvent for Product={ProductId}, SKU={Sku}",
            notification.ProductId, notification.Sku);

        var integrationEvent = new ProductCreatedIntegrationEvent(
            notification.ProductId,
            notification.Sku,
            notification.Name,
            notification.InitialStock,
            notification.OccurredAt);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
