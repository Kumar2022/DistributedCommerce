namespace Analytics.Domain.Aggregates;

/// <summary>
/// Aggregate for tracking customer lifetime value and behavior
/// </summary>
public class CustomerMetrics : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; }
    public int TotalOrders { get; private set; }
    public decimal LifetimeValue { get; private set; }
    public decimal AverageOrderValue { get; private set; }
    public DateTime FirstOrderDate { get; private set; }
    public DateTime? LastOrderDate { get; private set; }
    public int DaysSinceLastOrder { get; private set; }
    public string CustomerSegment { get; private set; } // VIP, Regular, At Risk, etc.
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CustomerMetrics() { } // For EF Core

    public CustomerMetrics(Guid customerId, string customerEmail)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        TotalOrders = 0;
        LifetimeValue = 0;
        AverageOrderValue = 0;
        FirstOrderDate = DateTime.UtcNow;
        DaysSinceLastOrder = 0;
        CustomerSegment = "New";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordOrder(decimal orderValue)
    {
        if (TotalOrders == 0)
        {
            FirstOrderDate = DateTime.UtcNow;
        }

        TotalOrders++;
        LifetimeValue += orderValue;
        LastOrderDate = DateTime.UtcNow;
        DaysSinceLastOrder = 0;
        RecalculateAverageOrderValue();
        UpdateCustomerSegment();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDaysSinceLastOrder()
    {
        if (!LastOrderDate.HasValue) return;
        DaysSinceLastOrder = (DateTime.UtcNow - LastOrderDate.Value).Days;
        UpdateCustomerSegment();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateAverageOrderValue()
    {
        AverageOrderValue = TotalOrders > 0 ? LifetimeValue / TotalOrders : 0;
    }

    private void UpdateCustomerSegment()
    {
        switch (LifetimeValue)
        {
            // Simple segmentation logic (can be made more sophisticated)
            case >= 10000:
                CustomerSegment = "VIP";
                break;
            case >= 5000:
                CustomerSegment = "High Value";
                break;
            default:
            {
                if (TotalOrders >= 5)
                    CustomerSegment = "Loyal";
                else if (DaysSinceLastOrder > 90)
                    CustomerSegment = "At Risk";
                else if (DaysSinceLastOrder > 180)
                    CustomerSegment = "Churned";
                else
                    CustomerSegment = "Regular";
                break;
            }
        }
    }
}
