namespace Analytics.Application.DTOs;

public record OrderMetricsDto(
    Guid Id,
    DateTime MetricDate,
    int TotalOrders,
    int CompletedOrders,
    int CancelledOrders,
    int PendingOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    string Currency
);

public record ProductMetricsDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    DateTime MetricDate,
    int ViewCount,
    int AddToCartCount,
    int PurchaseCount,
    decimal TotalRevenue,
    decimal ConversionRate,
    int InventoryLevel
);

public record CustomerMetricsDto(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    int TotalOrders,
    decimal LifetimeValue,
    decimal AverageOrderValue,
    DateTime FirstOrderDate,
    DateTime? LastOrderDate,
    int DaysSinceLastOrder,
    string CustomerSegment
);

public record RevenueMetricsDto(
    Guid Id,
    DateTime MetricDate,
    string Granularity,
    decimal TotalRevenue,
    decimal RefundedAmount,
    decimal NetRevenue,
    int TransactionCount,
    decimal AverageTransactionValue,
    string Currency
);

public record DashboardSummaryDto(
    decimal TodayRevenue,
    decimal MonthRevenue,
    int TodayOrders,
    int MonthOrders,
    int ActiveCustomers,
    decimal AverageOrderValue,
    decimal ConversionRate,
    List<TopProductDto> TopProducts
);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    int SalesCount,
    decimal Revenue
);

public record ConversionFunnelDto(
    int TotalVisitors,
    int ProductViews,
    int AddToCarts,
    int Checkouts,
    int Purchases,
    decimal ViewToCartRate,
    decimal CartToCheckoutRate,
    decimal CheckoutToPurchaseRate,
    decimal OverallConversionRate
);
