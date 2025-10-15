namespace Analytics.Domain.Aggregates;

/// <summary>
/// Aggregate for tracking order-related metrics over time
/// </summary>
public class OrderMetrics : AggregateRoot<Guid>
{
    public DateTime MetricDate { get; private set; }
    public int TotalOrders { get; private set; }
    public int CompletedOrders { get; private set; }
    public int CancelledOrders { get; private set; }
    public int PendingOrders { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal AverageOrderValue { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private OrderMetrics() { } // For EF Core

    public OrderMetrics(DateTime metricDate)
    {
        Id = Guid.NewGuid();
        MetricDate = metricDate.Date; // Normalize to date only
        TotalOrders = 0;
        CompletedOrders = 0;
        CancelledOrders = 0;
        PendingOrders = 0;
        TotalRevenue = 0;
        AverageOrderValue = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementTotalOrders(decimal orderValue)
    {
        TotalOrders++;
        TotalRevenue += orderValue;
        RecalculateAverage();
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementCompletedOrders()
    {
        CompletedOrders++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementCancelledOrders()
    {
        CancelledOrders++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementPendingOrders()
    {
        PendingOrders++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementPendingOrders()
    {
        if (PendingOrders <= 0) return;
        PendingOrders--;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateAverage()
    {
        AverageOrderValue = TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
    }
}
