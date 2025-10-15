using Analytics.Domain.Aggregates;

namespace Analytics.Domain.Repositories;

public interface IAnalyticsRepository
{
    // Order Metrics
    Task<OrderMetrics?> GetOrderMetricsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task AddOrderMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default);
    Task UpdateOrderMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderMetrics>> GetOrderMetricsRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Product Metrics
    Task<ProductMetrics?> GetProductMetricsAsync(Guid productId, DateTime date, CancellationToken cancellationToken = default);
    Task AddProductMetricsAsync(ProductMetrics metrics, CancellationToken cancellationToken = default);
    Task UpdateProductMetricsAsync(ProductMetrics metrics, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductMetrics>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int count, CancellationToken cancellationToken = default);

    // Customer Metrics
    Task<CustomerMetrics?> GetCustomerMetricsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddCustomerMetricsAsync(CustomerMetrics metrics, CancellationToken cancellationToken = default);
    Task UpdateCustomerMetricsAsync(CustomerMetrics metrics, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerMetrics>> GetCustomersBySegmentAsync(string segment, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerMetrics>> GetTopCustomersByValueAsync(int count, CancellationToken cancellationToken = default);

    // Revenue Metrics
    Task<RevenueMetrics?> GetRevenueMetricsAsync(DateTime date, TimeGranularity granularity, CancellationToken cancellationToken = default);
    Task AddRevenueMetricsAsync(RevenueMetrics metrics, CancellationToken cancellationToken = default);
    Task UpdateRevenueMetricsAsync(RevenueMetrics metrics, CancellationToken cancellationToken = default);
    Task<IEnumerable<RevenueMetrics>> GetRevenueMetricsRangeAsync(DateTime startDate, DateTime endDate, TimeGranularity granularity, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
