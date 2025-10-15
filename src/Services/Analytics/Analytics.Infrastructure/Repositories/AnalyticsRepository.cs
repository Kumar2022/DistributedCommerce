using Analytics.Infrastructure.Persistence;

namespace Analytics.Infrastructure.Repositories;

public class AnalyticsRepository(AnalyticsDbContext context, ILogger<AnalyticsRepository> logger)
    : IAnalyticsRepository
{
    private readonly ILogger<AnalyticsRepository> _logger = logger;

    // ========== Order Metrics ==========

    public async Task<OrderMetrics?> GetOrderMetricsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await context.OrderMetrics
            .FirstOrDefaultAsync(o => o.MetricDate == date.Date, cancellationToken);
    }

    public async Task AddOrderMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default)
    {
        await context.OrderMetrics.AddAsync(metrics, cancellationToken);
    }

    public async Task UpdateOrderMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default)
    {
        context.OrderMetrics.Update(metrics);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<OrderMetrics>> GetOrderMetricsRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        return await context.OrderMetrics
            .Where(o => o.MetricDate >= startDate.Date && o.MetricDate <= endDate.Date)
            .OrderBy(o => o.MetricDate)
            .ToListAsync(cancellationToken);
    }

    // ========== Product Metrics ==========

    public async Task<ProductMetrics?> GetProductMetricsAsync(
        Guid productId, 
        DateTime date, 
        CancellationToken cancellationToken = default)
    {
        return await context.ProductMetrics
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MetricDate == date.Date, cancellationToken);
    }

    public async Task AddProductMetricsAsync(ProductMetrics metrics, CancellationToken cancellationToken = default)
    {
        await context.ProductMetrics.AddAsync(metrics, cancellationToken);
    }

    public async Task UpdateProductMetricsAsync(ProductMetrics metrics, CancellationToken cancellationToken = default)
    {
        context.ProductMetrics.Update(metrics);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<ProductMetrics>> GetTopSellingProductsAsync(
        DateTime startDate, 
        DateTime endDate, 
        int count, 
        CancellationToken cancellationToken = default)
    {
        return await context.ProductMetrics
            .Where(p => p.MetricDate >= startDate.Date && p.MetricDate <= endDate.Date)
            .GroupBy(p => new { p.ProductId, p.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                TotalPurchases = g.Sum(p => p.PurchaseCount),
                TotalRevenue = g.Sum(p => p.TotalRevenue),
                LatestDate = g.Max(p => p.MetricDate)
            })
            .OrderByDescending(x => x.TotalPurchases)
            .Take(count)
            .Select(x => new ProductMetrics(x.ProductId, x.ProductName, x.LatestDate)
            {
                // These will be populated by the aggregation
            })
            .ToListAsync(cancellationToken);
    }

    // ========== Customer Metrics ==========

    public async Task<CustomerMetrics?> GetCustomerMetricsAsync(
        Guid customerId, 
        CancellationToken cancellationToken = default)
    {
        return await context.CustomerMetrics
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task AddCustomerMetricsAsync(CustomerMetrics metrics, CancellationToken cancellationToken = default)
    {
        await context.CustomerMetrics.AddAsync(metrics, cancellationToken);
    }

    public async Task UpdateCustomerMetricsAsync(CustomerMetrics metrics, CancellationToken cancellationToken = default)
    {
        context.CustomerMetrics.Update(metrics);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<CustomerMetrics>> GetCustomersBySegmentAsync(
        string segment, 
        CancellationToken cancellationToken = default)
    {
        return await context.CustomerMetrics
            .Where(c => c.CustomerSegment == segment)
            .OrderByDescending(c => c.LifetimeValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerMetrics>> GetTopCustomersByValueAsync(
        int count, 
        CancellationToken cancellationToken = default)
    {
        return await context.CustomerMetrics
            .OrderByDescending(c => c.LifetimeValue)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    // ========== Revenue Metrics ==========

    public async Task<RevenueMetrics?> GetRevenueMetricsAsync(
        DateTime date, 
        TimeGranularity granularity, 
        CancellationToken cancellationToken = default)
    {
        return await context.RevenueMetrics
            .FirstOrDefaultAsync(
                r => r.MetricDate == date.Date && r.Granularity == granularity, 
                cancellationToken);
    }

    public async Task AddRevenueMetricsAsync(RevenueMetrics metrics, CancellationToken cancellationToken = default)
    {
        await context.RevenueMetrics.AddAsync(metrics, cancellationToken);
    }

    public async Task UpdateRevenueMetricsAsync(RevenueMetrics metrics, CancellationToken cancellationToken = default)
    {
        context.RevenueMetrics.Update(metrics);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<RevenueMetrics>> GetRevenueMetricsRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        TimeGranularity granularity, 
        CancellationToken cancellationToken = default)
    {
        return await context.RevenueMetrics
            .Where(r => r.MetricDate >= startDate.Date 
                     && r.MetricDate <= endDate.Date 
                     && r.Granularity == granularity)
            .OrderBy(r => r.MetricDate)
            .ToListAsync(cancellationToken);
    }

    // ========== Unit of Work ==========

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}
