namespace Shipping.Application.EventHandlers;

// Integration events from Order Service
public record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    ShippingAddressDto ShippingAddress,
    List<OrderItemDto> Items,
    decimal TotalAmount
) : IntegrationEvent;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt
) : IntegrationEvent;

public class OrderConfirmedIntegrationEventHandler : IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderConfirmedIntegrationEventHandler> _logger;

    public OrderConfirmedIntegrationEventHandler(
        IMediator mediator,
        ILogger<OrderConfirmedIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(OrderConfirmedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing OrderConfirmed event for Order {OrderId}", @event.OrderId);

            // Generate tracking number
            var trackingNumber = GenerateTrackingNumber();
            
            // Determine carrier and delivery speed based on order value
            var carrier = DetermineCarrier(@event.TotalAmount);
            var deliverySpeed = DetermineDeliverySpeed(@event.TotalAmount);

            // Calculate package weight (simplified - in real scenario, get from product catalog)
            var packageWeight = @event.Items.Sum(i => i.Quantity) * 1.0m; // Assume 1kg per item
            
            // Create package
            var package = new PackageDto(
                Weight: packageWeight,
                Length: 30.0m,
                Width: 20.0m,
                Height: 15.0m,
                WeightUnit: "kg",
                DimensionUnit: "cm"
            );

            // Calculate shipping cost
            var shippingCost = CalculateShippingCost(deliverySpeed, packageWeight, @event.TotalAmount);

            // Create shipment command
            var command = new CreateShipmentCommand(
                OrderId: @event.OrderId,
                TrackingNumber: trackingNumber,
                Carrier: carrier,
                ShippingAddress: @event.ShippingAddress,
                Package: package,
                DeliverySpeed: deliverySpeed,
                ShippingCost: shippingCost,
                Currency: "USD"
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Shipment created successfully for Order {OrderId}: {ShipmentId}",
                    @event.OrderId, result.Value.Id);
            }
            else
            {
                _logger.LogError("Failed to create shipment for Order {OrderId}: {Error}",
                    @event.OrderId, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderConfirmed event for Order {OrderId}", @event.OrderId);
            throw;
        }
    }

    private static string GenerateTrackingNumber()
    {
        // Generate a tracking number in format: SHIP-YYYYMMDD-XXXXXX
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Random.Shared.Next(100000, 999999);
        return $"SHIP-{datePart}-{randomPart}";
    }

    private static string DetermineCarrier(decimal totalAmount)
    {
        // Select carrier based on order value
        return totalAmount switch
        {
            > 1000 => "FedEx Express",
            > 500 => "UPS",
            > 100 => "USPS Priority",
            _ => "USPS Standard"
        };
    }

    private static string DetermineDeliverySpeed(decimal totalAmount)
    {
        // Select delivery speed based on order value
        return totalAmount switch
        {
            > 1000 => "Express",
            > 500 => "TwoDay",
            > 100 => "Standard",
            _ => "Economy"
        };
    }

    private static decimal CalculateShippingCost(string deliverySpeed, decimal weight, decimal orderValue)
    {
        // Base cost calculation
        var baseCost = deliverySpeed switch
        {
            "Express" => 29.99m,
            "TwoDay" => 19.99m,
            "Standard" => 9.99m,
            "Economy" => 4.99m,
            _ => 9.99m
        };

        // Add weight-based cost ($1 per kg over 2kg)
        if (weight > 2.0m)
        {
            baseCost += (weight - 2.0m) * 1.0m;
        }

        // Free shipping for orders over $500
        if (orderValue > 500)
        {
            return 0m;
        }

        return baseCost;
    }
}

public class OrderCancelledIntegrationEventHandler : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCancelledIntegrationEventHandler> _logger;

    public OrderCancelledIntegrationEventHandler(
        IMediator mediator,
        ILogger<OrderCancelledIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCancelledIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing OrderCancelled event for Order {OrderId}", @event.OrderId);

            // Find shipments for this order
            var query = new GetShipmentsByOrderIdQuery(@event.OrderId);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess && result.Value.Any())
            {
                // Cancel all shipments for this order
                foreach (var shipment in result.Value)
                {
                    // Only cancel if not already delivered
                    if (shipment.Status != "Delivered" && shipment.Status != "Cancelled")
                    {
                        var cancelCommand = new CancelShipmentCommand(
                            Guid.Parse(shipment.Id.ToString()),
                            $"Order cancelled: {@event.Reason}"
                        );

                        var cancelResult = await _mediator.Send(cancelCommand, cancellationToken);

                        if (cancelResult.IsSuccess)
                        {
                            _logger.LogInformation("Shipment {ShipmentId} cancelled for Order {OrderId}",
                                shipment.Id, @event.OrderId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to cancel shipment {ShipmentId} for Order {OrderId}: {Error}",
                                shipment.Id, @event.OrderId, cancelResult.Error);
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("No shipments found for cancelled Order {OrderId}", @event.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderCancelled event for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}
