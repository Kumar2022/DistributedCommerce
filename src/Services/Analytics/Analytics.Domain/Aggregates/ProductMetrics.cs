namespace Analytics.Domain.Aggregates;

/// <summary>
/// Aggregate for tracking product performance metrics
/// </summary>
public class ProductMetrics : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public DateTime MetricDate { get; private set; }
    public int ViewCount { get; private set; }
    public int AddToCartCount { get; private set; }
    public int PurchaseCount { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal ConversionRate { get; private set; }
    public int InventoryLevel { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductMetrics() { } // For EF Core

    public ProductMetrics(Guid productId, string productName, DateTime metricDate)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        MetricDate = metricDate.Date;
        ViewCount = 0;
        AddToCartCount = 0;
        PurchaseCount = 0;
        TotalRevenue = 0;
        ConversionRate = 0;
        InventoryLevel = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViews()
    {
        ViewCount++;
        RecalculateConversionRate();
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementAddToCart()
    {
        AddToCartCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordPurchase(decimal revenue)
    {
        PurchaseCount++;
        TotalRevenue += revenue;
        RecalculateConversionRate();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInventoryLevel(int level)
    {
        InventoryLevel = level;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateConversionRate()
    {
        ConversionRate = ViewCount > 0 ? (decimal)PurchaseCount / ViewCount * 100 : 0;
    }
}
