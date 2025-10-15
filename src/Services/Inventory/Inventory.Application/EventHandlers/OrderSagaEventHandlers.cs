using BuildingBlocks.EventBus.Abstractions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.EventHandlers;

/// <summary>
/// Handles inventory reservation request from Order Saga
/// </summary>
public class InventoryReservationRequestedEventHandler : IIntegrationEventHandler<InventoryReservationRequestedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<InventoryReservationRequestedEventHandler> _logger;

    public InventoryReservationRequestedEventHandler(
        IProductRepository productRepository,
        IEventBus eventBus,
        ILogger<InventoryReservationRequestedEventHandler> logger)
    {
        _productRepository = productRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(InventoryReservationRequestedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing inventory reservation request for Order {OrderId}, ReservationId: {ReservationId}",
            @event.OrderId, @event.ReservationId);

        try
        {
            // Reserve stock for each item
            bool allReserved = true;
            var reservedItems = new List<(Guid ProductId, int Quantity)>();

            foreach (var item in @event.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                
                if (product == null)
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found for Order {OrderId}",
                        item.ProductId, @event.OrderId);
                    allReserved = false;
                    break;
                }

                var reserveResult = product.ReserveStock(@event.OrderId, item.Quantity, TimeSpan.FromMinutes(15));
                
                if (reserveResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to reserve stock for Product {ProductId}, Order {OrderId}: {Error}",
                        item.ProductId, @event.OrderId, reserveResult.Error.Message);
                    allReserved = false;
                    break;
                }

                await _productRepository.UpdateAsync(product, cancellationToken);
                reservedItems.Add((item.ProductId, item.Quantity));
                
                _logger.LogInformation(
                    "Reserved {Quantity} units of Product {ProductId} for Order {OrderId}",
                    item.Quantity, item.ProductId, @event.OrderId);
            }

            if (allReserved)
            {
                // Publish success event
                var confirmedEvent = new InventoryReservationConfirmedEvent(
                    OrderId: @event.OrderId,
                    ReservationId: @event.ReservationId,
                    Items: @event.Items,
                    ConfirmedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(confirmedEvent, cancellationToken);

                _logger.LogInformation(
                    "Inventory reservation confirmed for Order {OrderId}, ReservationId: {ReservationId}",
                    @event.OrderId, @event.ReservationId);
            }
            else
            {
                // Rollback partial reservations
                foreach (var (productId, quantity) in reservedItems)
                {
                    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                    if (product != null)
                    {
                        product.ReleaseReservation(@event.OrderId);
                        await _productRepository.UpdateAsync(product, cancellationToken);
                    }
                }

                // Publish failure event
                var failedEvent = new InventoryReservationFailedEvent(
                    OrderId: @event.OrderId,
                    ReservationId: @event.ReservationId,
                    Reason: "Insufficient stock or product not found",
                    FailedAt: DateTime.UtcNow
                );

                await _eventBus.PublishAsync(failedEvent, cancellationToken);

                _logger.LogWarning(
                    "Inventory reservation failed for Order {OrderId}, ReservationId: {ReservationId}",
                    @event.OrderId, @event.ReservationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing inventory reservation for Order {OrderId}, ReservationId: {ReservationId}",
                @event.OrderId, @event.ReservationId);

            // Publish failure event
            var failedEvent = new InventoryReservationFailedEvent(
                OrderId: @event.OrderId,
                ReservationId: @event.ReservationId,
                Reason: $"Error: {ex.Message}",
                FailedAt: DateTime.UtcNow
            );

            await _eventBus.PublishAsync(failedEvent, cancellationToken);
            throw;
        }
    }
}

/// <summary>
/// Handles inventory reservation release from Order Saga (compensation)
/// </summary>
public class InventoryReservationReleasedEventHandler : IIntegrationEventHandler<InventoryReservationReleasedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<InventoryReservationReleasedEventHandler> _logger;

    public InventoryReservationReleasedEventHandler(
        IProductRepository productRepository,
        ILogger<InventoryReservationReleasedEventHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task HandleAsync(InventoryReservationReleasedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing inventory reservation release for Order {OrderId}, ReservationId: {ReservationId}, Reason: {Reason}",
            @event.OrderId, @event.ReservationId, @event.Reason);

        try
        {
            // Find all products with reservations for this order
            var products = await _productRepository.GetByOrderIdAsync(@event.OrderId, cancellationToken);

            foreach (var product in products)
            {
                var releaseResult = product.ReleaseReservation(@event.OrderId);
                
                if (releaseResult.IsSuccess)
                {
                    await _productRepository.UpdateAsync(product, cancellationToken);
                    
                    _logger.LogInformation(
                        "Released reservation for Product {ProductId}, Order {OrderId}",
                        product.ProductId, @event.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to release reservation for Product {ProductId}, Order {OrderId}: {Error}",
                        product.ProductId, @event.OrderId, releaseResult.Error.Message);
                }
            }

            _logger.LogInformation(
                "Inventory reservation released for Order {OrderId}, ReservationId: {ReservationId}",
                @event.OrderId, @event.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error releasing inventory reservation for Order {OrderId}, ReservationId: {ReservationId}",
                @event.OrderId, @event.ReservationId);
            throw;
        }
    }
}

// Event definitions matching the Order Saga
public record InventoryReservationRequestedEvent(
    Guid OrderId,
    Guid CustomerId,
    Guid ReservationId,
    List<InventoryReservationItem> Items,
    DateTime RequestedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationReleasedEvent(
    Guid OrderId,
    Guid ReservationId,
    string Reason,
    DateTime ReleasedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationItem(
    Guid ProductId,
    int Quantity
);

// Response events
public record InventoryReservationConfirmedEvent(
    Guid OrderId,
    Guid ReservationId,
    List<InventoryReservationItem> Items,
    DateTime ConfirmedAt
) : IntegrationEvent(OrderId);

public record InventoryReservationFailedEvent(
    Guid OrderId,
    Guid ReservationId,
    string Reason,
    DateTime FailedAt
) : IntegrationEvent(OrderId);
