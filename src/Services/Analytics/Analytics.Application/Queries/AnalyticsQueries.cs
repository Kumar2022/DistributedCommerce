using Analytics.Application.DTOs;

namespace Analytics.Application.Queries;

// ========== Order Analytics Queries ==========

public record GetOrderSummaryQuery(DateTime? StartDate = null, DateTime? EndDate = null) 
    : IRequest<Result<OrderMetricsDto>>;

public class GetOrderSummaryQueryHandler(IAnalyticsRepository repository, ILogger<GetOrderSummaryQueryHandler> logger)
    : IRequestHandler<GetOrderSummaryQuery, Result<OrderMetricsDto>>
{
    public async Task<Result<OrderMetricsDto>> Handle(GetOrderSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.Date;
            var endDate = request.EndDate ?? DateTime.UtcNow.Date;

            var metrics = await repository.GetOrderMetricsRangeAsync(startDate, endDate, cancellationToken);
            var aggregated = AggregateOrderMetrics(metrics, startDate);

            var dto = new OrderMetricsDto(
                aggregated.Id,
                aggregated.MetricDate,
                aggregated.TotalOrders,
                aggregated.CompletedOrders,
                aggregated.CancelledOrders,
                aggregated.PendingOrders,
                aggregated.TotalRevenue,
                aggregated.AverageOrderValue,
                aggregated.Currency
            );

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order summary");
            return Result.Failure<OrderMetricsDto>(new Error("Analytics.Order.Error", ex.Message));
        }
    }

    private OrderMetrics AggregateOrderMetrics(IEnumerable<OrderMetrics> metricsList, DateTime date)
    {
        var aggregated = new OrderMetrics(date);
        foreach (var metric in metricsList)
        {
            for (var i = 0; i < metric.TotalOrders; i++)
            {
                aggregated.IncrementTotalOrders(metric.TotalRevenue / Math.Max(metric.TotalOrders, 1));
            }
            for (var i = 0; i < metric.CompletedOrders; i++) aggregated.IncrementCompletedOrders();
            for (var i = 0; i < metric.CancelledOrders; i++) aggregated.IncrementCancelledOrders();
        }
        return aggregated;
    }
}

// ========== Revenue Analytics Queries ==========

public record GetDailyRevenueQuery(DateTime Date) 
    : IRequest<Result<RevenueMetricsDto>>;

public class GetDailyRevenueQueryHandler(IAnalyticsRepository repository, ILogger<GetDailyRevenueQueryHandler> logger)
    : IRequestHandler<GetDailyRevenueQuery, Result<RevenueMetricsDto>>
{
    public async Task<Result<RevenueMetricsDto>> Handle(GetDailyRevenueQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await repository.GetRevenueMetricsAsync(
                request.Date.Date, 
                TimeGranularity.Daily, 
                cancellationToken);

            if (metrics == null)
            {
                return Result.Failure<RevenueMetricsDto>(new Error("Revenue.NotFound", "No revenue data for this date"));
            }

            var dto = new RevenueMetricsDto(
                metrics.Id,
                metrics.MetricDate,
                metrics.Granularity.ToString(),
                metrics.TotalRevenue,
                metrics.RefundedAmount,
                metrics.NetRevenue,
                metrics.TransactionCount,
                metrics.AverageTransactionValue,
                metrics.Currency
            );

            return Result<RevenueMetricsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving daily revenue");
            return Result.Failure<RevenueMetricsDto>(new Error("Analytics.Revenue.Error", ex.Message));
        }
    }
}

public record GetMonthlyRevenueQuery(int Year, int Month) 
    : IRequest<Result<List<RevenueMetricsDto>>>;

public class GetMonthlyRevenueQueryHandler(
    IAnalyticsRepository repository,
    ILogger<GetMonthlyRevenueQueryHandler> logger)
    : IRequestHandler<GetMonthlyRevenueQuery, Result<List<RevenueMetricsDto>>>
{
    public async Task<Result<List<RevenueMetricsDto>>> Handle(GetMonthlyRevenueQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var startDate = new DateTime(request.Year, request.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var metrics = await repository.GetRevenueMetricsRangeAsync(
                startDate, 
                endDate, 
                TimeGranularity.Daily, 
                cancellationToken);

            var dtos = metrics.Select(m => new RevenueMetricsDto(
                m.Id,
                m.MetricDate,
                m.Granularity.ToString(),
                m.TotalRevenue,
                m.RefundedAmount,
                m.NetRevenue,
                m.TransactionCount,
                m.AverageTransactionValue,
                m.Currency
            )).ToList();

            return Result<List<RevenueMetricsDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving monthly revenue");
            return Result.Failure<List<RevenueMetricsDto>>(new Error("Analytics.Revenue.Error", ex.Message));
        }
    }
}

// ========== Product Analytics Queries ==========

public record GetTopSellingProductsQuery(DateTime StartDate, DateTime EndDate, int Count = 10) 
    : IRequest<Result<List<ProductMetricsDto>>>;

public class GetTopSellingProductsQueryHandler(
    IAnalyticsRepository repository,
    ILogger<GetTopSellingProductsQueryHandler> logger)
    : IRequestHandler<GetTopSellingProductsQuery, Result<List<ProductMetricsDto>>>
{
    public async Task<Result<List<ProductMetricsDto>>> Handle(GetTopSellingProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await repository.GetTopSellingProductsAsync(
                request.StartDate, 
                request.EndDate, 
                request.Count, 
                cancellationToken);

            var dtos = metrics.Select(m => new ProductMetricsDto(
                m.Id,
                m.ProductId,
                m.ProductName,
                m.MetricDate,
                m.ViewCount,
                m.AddToCartCount,
                m.PurchaseCount,
                m.TotalRevenue,
                m.ConversionRate,
                m.InventoryLevel
            )).ToList();

            return Result<List<ProductMetricsDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving top selling products");
            return Result.Failure<List<ProductMetricsDto>>(new Error("Analytics.Product.Error", ex.Message));
        }
    }
}

// ========== Customer Analytics Queries ==========

public record GetCustomerLifetimeValueQuery(Guid CustomerId) 
    : IRequest<Result<CustomerMetricsDto>>;

public class GetCustomerLifetimeValueQueryHandler(
    IAnalyticsRepository repository,
    ILogger<GetCustomerLifetimeValueQueryHandler> logger)
    : IRequestHandler<GetCustomerLifetimeValueQuery, Result<CustomerMetricsDto>>
{
    public async Task<Result<CustomerMetricsDto>> Handle(GetCustomerLifetimeValueQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await repository.GetCustomerMetricsAsync(request.CustomerId, cancellationToken);

            if (metrics == null)
            {
                return Result.Failure<CustomerMetricsDto>(new Error("Customer.NotFound", "Customer metrics not found"));
            }

            var dto = new CustomerMetricsDto(
                metrics.Id,
                metrics.CustomerId,
                metrics.CustomerEmail,
                metrics.TotalOrders,
                metrics.LifetimeValue,
                metrics.AverageOrderValue,
                metrics.FirstOrderDate,
                metrics.LastOrderDate,
                metrics.DaysSinceLastOrder,
                metrics.CustomerSegment
            );

            return Result<CustomerMetricsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customer lifetime value");
            return Result.Failure<CustomerMetricsDto>(new Error("Analytics.Customer.Error", ex.Message));
        }
    }
}

// ========== Dashboard Query ==========

public record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler(
    IAnalyticsRepository repository,
    ILogger<GetDashboardSummaryQueryHandler> logger)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // Get today's revenue
            var todayRevenue = await repository.GetRevenueMetricsAsync(today, TimeGranularity.Daily, cancellationToken);
            
            // Get month's revenue
            var monthRevenue = await repository.GetRevenueMetricsRangeAsync(monthStart, today, TimeGranularity.Daily, cancellationToken);
            
            // Get today's orders
            var todayOrders = await repository.GetOrderMetricsByDateAsync(today, cancellationToken);
            
            // Get month's orders
            var monthOrders = await repository.GetOrderMetricsRangeAsync(monthStart, today, cancellationToken);

            // Get top products
            var topProducts = await repository.GetTopSellingProductsAsync(monthStart, today, 5, cancellationToken);

            var summary = new DashboardSummaryDto(
                TodayRevenue: todayRevenue?.NetRevenue ?? 0,
                MonthRevenue: monthRevenue.Sum(r => r.NetRevenue),
                TodayOrders: todayOrders?.TotalOrders ?? 0,
                MonthOrders: monthOrders.Sum(o => o.TotalOrders),
                ActiveCustomers: 0, // Would need customer count query
                AverageOrderValue: monthOrders.Any() ? monthOrders.Average(o => o.AverageOrderValue) : 0,
                ConversionRate: 0, // Would need conversion tracking
                TopProducts: topProducts.Select(p => new TopProductDto(
                    p.ProductId,
                    p.ProductName,
                    p.PurchaseCount,
                    p.TotalRevenue
                )).ToList()
            );

            return Result<DashboardSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard summary");
            return Result.Failure<DashboardSummaryDto>(new Error("Analytics.Dashboard.Error", ex.Message));
        }
    }
}

// ========== Conversion Funnel Query ==========

public record GetConversionFunnelQuery(DateTime StartDate, DateTime EndDate) 
    : IRequest<Result<ConversionFunnelDto>>;

public class GetConversionFunnelQueryHandler(
    IAnalyticsRepository repository,
    ILogger<GetConversionFunnelQueryHandler> logger)
    : IRequestHandler<GetConversionFunnelQuery, Result<ConversionFunnelDto>>
{
    public async Task<Result<ConversionFunnelDto>> Handle(GetConversionFunnelQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // This is a simplified version - in production would track actual funnel steps
            var topProducts = await repository.GetTopSellingProductsAsync(request.StartDate, request.EndDate, 100, cancellationToken);
            
            var totalViews = topProducts.Sum(p => p.ViewCount);
            var totalAddToCarts = topProducts.Sum(p => p.AddToCartCount);
            var totalPurchases = topProducts.Sum(p => p.PurchaseCount);

            var funnel = new ConversionFunnelDto(
                TotalVisitors: totalViews,
                ProductViews: totalViews,
                AddToCarts: totalAddToCarts,
                Checkouts: totalPurchases, // Simplified - assuming checkout = purchase
                Purchases: totalPurchases,
                ViewToCartRate: totalViews > 0 ? (decimal)totalAddToCarts / totalViews * 100 : 0,
                CartToCheckoutRate: totalAddToCarts > 0 ? (decimal)totalPurchases / totalAddToCarts * 100 : 0,
                CheckoutToPurchaseRate: 100, // Simplified
                OverallConversionRate: totalViews > 0 ? (decimal)totalPurchases / totalViews * 100 : 0
            );

            return Result<ConversionFunnelDto>.Success(funnel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving conversion funnel");
            return Result.Failure<ConversionFunnelDto>(new Error("Analytics.Funnel.Error", ex.Message));
        }
    }
}
