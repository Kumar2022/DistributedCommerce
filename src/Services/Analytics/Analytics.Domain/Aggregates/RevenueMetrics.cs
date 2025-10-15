namespace Analytics.Domain.Aggregates;

/// <summary>
/// Aggregate for tracking revenue metrics with time-based aggregations
/// </summary>
public class RevenueMetrics : AggregateRoot<Guid>
{
    public DateTime MetricDate { get; private set; }
    public TimeGranularity Granularity { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public decimal NetRevenue { get; private set; }
    public int TransactionCount { get; private set; }
    public decimal AverageTransactionValue { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private RevenueMetrics() { } // For EF Core

    public RevenueMetrics(DateTime metricDate, TimeGranularity granularity)
    {
        Id = Guid.NewGuid();
        MetricDate = metricDate.Date;
        Granularity = granularity;
        TotalRevenue = 0;
        RefundedAmount = 0;
        NetRevenue = 0;
        TransactionCount = 0;
        AverageTransactionValue = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordRevenue(decimal amount)
    {
        TotalRevenue += amount;
        TransactionCount++;
        RecalculateMetrics();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordRefund(decimal amount)
    {
        RefundedAmount += amount;
        RecalculateMetrics();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateMetrics()
    {
        NetRevenue = TotalRevenue - RefundedAmount;
        AverageTransactionValue = TransactionCount > 0 ? TotalRevenue / TransactionCount : 0;
    }
}

public enum TimeGranularity
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}
