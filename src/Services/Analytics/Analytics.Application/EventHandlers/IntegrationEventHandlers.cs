using BuildingBlocks.EventBus.Abstractions;
using Analytics.Application.IntegrationEvents;

namespace Analytics.Application.EventHandlers;

// ========== Order Event Handlers ==========

public class OrderCreatedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<OrderCreatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.CreatedAt.Date;

            // Update order metrics
            var orderMetrics = await repository.GetOrderMetricsByDateAsync(date, cancellationToken);
            if (orderMetrics == null)
            {
                orderMetrics = new OrderMetrics(date);
                await repository.AddOrderMetricsAsync(orderMetrics, cancellationToken);
            }
            
            orderMetrics.IncrementTotalOrders(@event.TotalAmount);
            orderMetrics.IncrementPendingOrders();
            await repository.UpdateOrderMetricsAsync(orderMetrics, cancellationToken);

            // Update customer metrics
            var customerMetrics = await repository.GetCustomerMetricsAsync(@event.CustomerId, cancellationToken);
            if (customerMetrics == null)
            {
                customerMetrics = new CustomerMetrics(@event.CustomerId, @event.CustomerEmail);
                await repository.AddCustomerMetricsAsync(customerMetrics, cancellationToken);
            }
            
            customerMetrics.RecordOrder(@event.TotalAmount);
            await repository.UpdateCustomerMetricsAsync(customerMetrics, cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed OrderCreatedIntegrationEvent for Order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing OrderCreatedIntegrationEvent for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

public class OrderConfirmedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<OrderConfirmedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    public async Task HandleAsync(OrderConfirmedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.ConfirmedAt.Date;
            var orderMetrics = await repository.GetOrderMetricsByDateAsync(date, cancellationToken);
            
            if (orderMetrics != null)
            {
                orderMetrics.IncrementCompletedOrders();
                orderMetrics.DecrementPendingOrders();
                await repository.UpdateOrderMetricsAsync(orderMetrics, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Processed OrderConfirmedIntegrationEvent for Order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing OrderConfirmedIntegrationEvent for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

public class OrderCancelledIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<OrderCancelledIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    public async Task HandleAsync(OrderCancelledIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.CancelledAt.Date;
            var orderMetrics = await repository.GetOrderMetricsByDateAsync(date, cancellationToken);
            
            if (orderMetrics != null)
            {
                orderMetrics.IncrementCancelledOrders();
                orderMetrics.DecrementPendingOrders();
                await repository.UpdateOrderMetricsAsync(orderMetrics, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Processed OrderCancelledIntegrationEvent for Order {OrderId}", @event.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing OrderCancelledIntegrationEvent for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}

// ========== Payment Event Handlers ==========

public class PaymentCompletedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<PaymentCompletedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<PaymentCompletedIntegrationEvent>
{
    public async Task HandleAsync(PaymentCompletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.CompletedAt.Date;
            var revenueMetrics = await repository.GetRevenueMetricsAsync(date, TimeGranularity.Daily, cancellationToken);
            
            if (revenueMetrics == null)
            {
                revenueMetrics = new RevenueMetrics(date, TimeGranularity.Daily);
                await repository.AddRevenueMetricsAsync(revenueMetrics, cancellationToken);
            }
            
            revenueMetrics.RecordRevenue(@event.Amount);
            await repository.UpdateRevenueMetricsAsync(revenueMetrics, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed PaymentCompletedIntegrationEvent for Payment {PaymentId}", @event.PaymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PaymentCompletedIntegrationEvent for Payment {PaymentId}", @event.PaymentId);
            throw;
        }
    }
}

public class PaymentRefundedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<PaymentRefundedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<PaymentRefundedIntegrationEvent>
{
    public async Task HandleAsync(PaymentRefundedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.RefundedAt.Date;
            var revenueMetrics = await repository.GetRevenueMetricsAsync(date, TimeGranularity.Daily, cancellationToken);
            
            if (revenueMetrics == null)
            {
                revenueMetrics = new RevenueMetrics(date, TimeGranularity.Daily);
                await repository.AddRevenueMetricsAsync(revenueMetrics, cancellationToken);
            }
            
            revenueMetrics.RecordRefund(@event.Amount);
            await repository.UpdateRevenueMetricsAsync(revenueMetrics, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed PaymentRefundedIntegrationEvent for Payment {PaymentId}", @event.PaymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PaymentRefundedIntegrationEvent for Payment {PaymentId}", @event.PaymentId);
            throw;
        }
    }
}

// ========== Product Event Handlers ==========

public class ProductViewedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<ProductViewedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ProductViewedIntegrationEvent>
{
    public async Task HandleAsync(ProductViewedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.ViewedAt.Date;
            var productMetrics = await repository.GetProductMetricsAsync(@event.ProductId, date, cancellationToken);
            
            if (productMetrics == null)
            {
                productMetrics = new ProductMetrics(@event.ProductId, @event.ProductName, date);
                await repository.AddProductMetricsAsync(productMetrics, cancellationToken);
            }
            
            productMetrics.IncrementViews();
            await repository.UpdateProductMetricsAsync(productMetrics, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed ProductViewedIntegrationEvent for Product {ProductId}", @event.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ProductViewedIntegrationEvent for Product {ProductId}", @event.ProductId);
            throw;
        }
    }
}

public class ProductAddedToCartIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<ProductAddedToCartIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ProductAddedToCartIntegrationEvent>
{
    public async Task HandleAsync(ProductAddedToCartIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.AddedAt.Date;
            var productMetrics = await repository.GetProductMetricsAsync(@event.ProductId, date, cancellationToken);
            
            if (productMetrics == null)
            {
                productMetrics = new ProductMetrics(@event.ProductId, @event.ProductName, date);
                await repository.AddProductMetricsAsync(productMetrics, cancellationToken);
            }
            
            productMetrics.IncrementAddToCart();
            await repository.UpdateProductMetricsAsync(productMetrics, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed ProductAddedToCartIntegrationEvent for Product {ProductId}", @event.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ProductAddedToCartIntegrationEvent for Product {ProductId}", @event.ProductId);
            throw;
        }
    }
}

public class ProductPurchasedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<ProductPurchasedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ProductPurchasedIntegrationEvent>
{
    public async Task HandleAsync(ProductPurchasedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.PurchasedAt.Date;
            var productMetrics = await repository.GetProductMetricsAsync(@event.ProductId, date, cancellationToken);
            
            if (productMetrics == null)
            {
                productMetrics = new ProductMetrics(@event.ProductId, @event.ProductName, date);
                await repository.AddProductMetricsAsync(productMetrics, cancellationToken);
            }
            
            var revenue = @event.Price * @event.Quantity;
            productMetrics.RecordPurchase(revenue);
            await repository.UpdateProductMetricsAsync(productMetrics, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Processed ProductPurchasedIntegrationEvent for Product {ProductId}", @event.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ProductPurchasedIntegrationEvent for Product {ProductId}", @event.ProductId);
            throw;
        }
    }
}

public class StockAdjustedIntegrationEventHandler(
    IAnalyticsRepository repository,
    ILogger<StockAdjustedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StockAdjustedIntegrationEvent>
{
    public async Task HandleAsync(StockAdjustedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var date = @event.AdjustedAt.Date;
            var productMetrics = await repository.GetProductMetricsAsync(@event.ProductId, date, cancellationToken);
            
            if (productMetrics != null)
            {
                productMetrics.UpdateInventoryLevel(@event.NewQuantity);
                await repository.UpdateProductMetricsAsync(productMetrics, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Processed StockAdjustedIntegrationEvent for Product {ProductId}", @event.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing StockAdjustedIntegrationEvent for Product {ProductId}", @event.ProductId);
            throw;
        }
    }
}
