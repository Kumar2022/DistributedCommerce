using Analytics.Domain.Aggregates;

namespace Analytics.UnitTests.Domain;

/// <summary>
/// Unit tests for ProductMetrics aggregate
/// Tests product performance tracking and conversion rate calculations
/// </summary>
public class ProductMetricsTests
{
    [Fact(DisplayName = "Constructor: Should create with valid parameters")]
    public void Constructor_WithValidParameters_ShouldCreateProductMetrics()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var metricDate = new DateTime(2024, 1, 15);

        // Act
        var metrics = new ProductMetrics(productId, productName, metricDate);

        // Assert
        metrics.Should().NotBeNull();
        metrics.ProductId.Should().Be(productId);
        metrics.ProductName.Should().Be(productName);
        metrics.MetricDate.Should().Be(metricDate.Date);
        metrics.ViewCount.Should().Be(0);
        metrics.AddToCartCount.Should().Be(0);
        metrics.PurchaseCount.Should().Be(0);
        metrics.TotalRevenue.Should().Be(0);
        metrics.ConversionRate.Should().Be(0);
        metrics.InventoryLevel.Should().Be(0);
    }

    [Fact(DisplayName = "IncrementViews: Should increment view count")]
    public void IncrementViews_ShouldIncrementCount()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.IncrementViews();
        metrics.IncrementViews();

        // Assert
        metrics.ViewCount.Should().Be(2);
    }

    [Fact(DisplayName = "IncrementAddToCart: Should increment cart count")]
    public void IncrementAddToCart_ShouldIncrementCount()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.IncrementAddToCart();

        // Assert
        metrics.AddToCartCount.Should().Be(1);
    }

    [Fact(DisplayName = "RecordPurchase: Should increment purchase and revenue")]
    public void RecordPurchase_ShouldIncrementPurchaseAndRevenue()
    {
        // Arrange
        var metrics = CreateTestMetrics();
        var revenue = 99.99m;

        // Act
        metrics.RecordPurchase(revenue);

        // Assert
        metrics.PurchaseCount.Should().Be(1);
        metrics.TotalRevenue.Should().Be(revenue);
    }

    [Fact(DisplayName = "RecordPurchase: Should calculate conversion rate correctly")]
    public void RecordPurchase_WithViews_ShouldCalculateConversionRate()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - 100 views, 10 purchases = 10% conversion
        for (int i = 0; i < 100; i++)
        {
            metrics.IncrementViews();
        }
        
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordPurchase(50m);
        }

        // Assert
        metrics.ViewCount.Should().Be(100);
        metrics.PurchaseCount.Should().Be(10);
        metrics.ConversionRate.Should().Be(10m); // 10%
        metrics.TotalRevenue.Should().Be(500m);
    }

    [Fact(DisplayName = "UpdateInventoryLevel: Should update inventory")]
    public void UpdateInventoryLevel_ShouldUpdateLevel()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.UpdateInventoryLevel(50);

        // Assert
        metrics.InventoryLevel.Should().Be(50);
    }

    [Fact(DisplayName = "Scenario: Product funnel tracking")]
    public void Scenario_ProductFunnel_ShouldTrackCorrectly()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - Simulate funnel: 1000 views → 200 add to cart → 50 purchases
        for (int i = 0; i < 1000; i++)
        {
            metrics.IncrementViews();
        }

        for (int i = 0; i < 200; i++)
        {
            metrics.IncrementAddToCart();
        }

        for (int i = 0; i < 50; i++)
        {
            metrics.RecordPurchase(100m);
        }

        // Assert
        metrics.ViewCount.Should().Be(1000);
        metrics.AddToCartCount.Should().Be(200);
        metrics.PurchaseCount.Should().Be(50);
        metrics.ConversionRate.Should().Be(5m); // 5% conversion
        metrics.TotalRevenue.Should().Be(5000m);
    }

    private static ProductMetrics CreateTestMetrics()
    {
        return new ProductMetrics(Guid.NewGuid(), "Test Product", DateTime.Today);
    }
}

/// <summary>
/// Unit tests for CustomerMetrics aggregate
/// Tests customer lifetime value and segmentation
/// </summary>
public class CustomerMetricsTests
{
    [Fact(DisplayName = "Constructor: Should create with valid parameters")]
    public void Constructor_WithValidParameters_ShouldCreateCustomerMetrics()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var metrics = new CustomerMetrics(customerId, email);

        // Assert
        metrics.Should().NotBeNull();
        metrics.CustomerId.Should().Be(customerId);
        metrics.CustomerEmail.Should().Be(email);
        metrics.TotalOrders.Should().Be(0);
        metrics.LifetimeValue.Should().Be(0);
        metrics.AverageOrderValue.Should().Be(0);
        metrics.DaysSinceLastOrder.Should().Be(0);
        metrics.CustomerSegment.Should().Be("New");
    }

    [Fact(DisplayName = "RecordOrder: First order should set FirstOrderDate")]
    public void RecordOrder_FirstOrder_ShouldSetFirstOrderDate()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.RecordOrder(100m);

        // Assert
        metrics.TotalOrders.Should().Be(1);
        metrics.LifetimeValue.Should().Be(100m);
        metrics.AverageOrderValue.Should().Be(100m);
        metrics.FirstOrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metrics.LastOrderDate.Should().HaveValue();
    }

    [Fact(DisplayName = "RecordOrder: Should calculate average order value correctly")]
    public void RecordOrder_MultipleOrders_ShouldCalculateAverageCorrectly()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.RecordOrder(100m);
        metrics.RecordOrder(200m);
        metrics.RecordOrder(300m);

        // Assert
        metrics.TotalOrders.Should().Be(3);
        metrics.LifetimeValue.Should().Be(600m);
        metrics.AverageOrderValue.Should().Be(200m);
    }

    [Fact(DisplayName = "RecordOrder: Should update customer segment to VIP")]
    public void RecordOrder_HighValue_ShouldSegmentAsVIP()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - Spend $10,000+
        metrics.RecordOrder(10000m);

        // Assert
        metrics.CustomerSegment.Should().Be("VIP");
    }

    [Fact(DisplayName = "RecordOrder: Should update customer segment to High Value")]
    public void RecordOrder_MediumHighValue_ShouldSegmentAsHighValue()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - Spend $5,000-$9,999
        metrics.RecordOrder(5000m);

        // Assert
        metrics.CustomerSegment.Should().Be("High Value");
    }

    [Fact(DisplayName = "RecordOrder: Should update customer segment to Loyal")]
    public void RecordOrder_MultipleOrders_ShouldSegmentAsLoyal()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - 5+ orders
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordOrder(100m);
        }

        // Assert
        metrics.CustomerSegment.Should().Be("Loyal");
    }

    [Fact(DisplayName = "UpdateDaysSinceLastOrder: Should calculate days correctly")]
    public void UpdateDaysSinceLastOrder_WithLastOrder_ShouldCalculateDays()
    {
        // Arrange
        var metrics = CreateTestMetrics();
        metrics.RecordOrder(100m);

        // Act
        Thread.Sleep(10); // Small delay
        metrics.UpdateDaysSinceLastOrder();

        // Assert
        metrics.DaysSinceLastOrder.Should().Be(0); // Same day
    }

    [Fact(DisplayName = "Scenario: Customer lifecycle from new to VIP")]
    public void Scenario_CustomerLifecycle_ShouldProgressCorrectly()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act - Customer journey
        // New customer
        metrics.CustomerSegment.Should().Be("New");

        // First order ($100, total: $100, orders: 1)
        metrics.RecordOrder(100m);

        // Multiple orders ($150 each, total: $100 + $600 = $700, orders: 5)
        for (int i = 0; i < 4; i++)
        {
            metrics.RecordOrder(150m);
        }
        metrics.TotalOrders.Should().Be(5);
        metrics.CustomerSegment.Should().Be("Loyal"); // 5+ orders

        // Large order pushes to High Value ($700 + $4300 = $5000, orders: 6)
        metrics.RecordOrder(4300m);
        metrics.LifetimeValue.Should().Be(5000m);
        metrics.CustomerSegment.Should().Be("High Value");

        // Another large order makes VIP ($5000 + $5000 = $10,000, orders: 7)
        metrics.RecordOrder(5000m);
        metrics.CustomerSegment.Should().Be("VIP");

        // Assert
        metrics.TotalOrders.Should().Be(7);
        metrics.LifetimeValue.Should().Be(10000m);
        metrics.CustomerSegment.Should().Be("VIP");
    }

    private static CustomerMetrics CreateTestMetrics()
    {
        return new CustomerMetrics(Guid.NewGuid(), "test@example.com");
    }
}

/// <summary>
/// Unit tests for RevenueMetrics aggregate
/// Tests revenue tracking and refund handling
/// </summary>
public class RevenueMetricsTests
{
    [Fact(DisplayName = "Constructor: Should create with valid parameters")]
    public void Constructor_WithValidParameters_ShouldCreateRevenueMetrics()
    {
        // Arrange
        var metricDate = new DateTime(2024, 1, 15);
        var granularity = TimeGranularity.Daily;

        // Act
        var metrics = new RevenueMetrics(metricDate, granularity);

        // Assert
        metrics.Should().NotBeNull();
        metrics.MetricDate.Should().Be(metricDate.Date);
        metrics.Granularity.Should().Be(granularity);
        metrics.TotalRevenue.Should().Be(0);
        metrics.RefundedAmount.Should().Be(0);
        metrics.NetRevenue.Should().Be(0);
        metrics.TransactionCount.Should().Be(0);
        metrics.AverageTransactionValue.Should().Be(0);
        metrics.Currency.Should().Be("USD");
    }

    [Fact(DisplayName = "RecordRevenue: Should increment revenue and transaction count")]
    public void RecordRevenue_ShouldIncrementRevenueAndCount()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.RecordRevenue(100m);

        // Assert
        metrics.TotalRevenue.Should().Be(100m);
        metrics.TransactionCount.Should().Be(1);
        metrics.NetRevenue.Should().Be(100m);
        metrics.AverageTransactionValue.Should().Be(100m);
    }

    [Fact(DisplayName = "RecordRevenue: Should calculate average transaction value")]
    public void RecordRevenue_MultipleTransactions_ShouldCalculateAverage()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        metrics.RecordRevenue(100m);
        metrics.RecordRevenue(200m);
        metrics.RecordRevenue(300m);

        // Assert
        metrics.TotalRevenue.Should().Be(600m);
        metrics.TransactionCount.Should().Be(3);
        metrics.AverageTransactionValue.Should().Be(200m);
    }

    [Fact(DisplayName = "RecordRefund: Should subtract from net revenue")]
    public void RecordRefund_ShouldSubtractFromNetRevenue()
    {
        // Arrange
        var metrics = CreateTestMetrics();
        metrics.RecordRevenue(1000m);

        // Act
        metrics.RecordRefund(100m);

        // Assert
        metrics.TotalRevenue.Should().Be(1000m);
        metrics.RefundedAmount.Should().Be(100m);
        metrics.NetRevenue.Should().Be(900m);
    }

    [Fact(DisplayName = "RecordRefund: Multiple refunds should accumulate")]
    public void RecordRefund_MultipleRefunds_ShouldAccumulate()
    {
        // Arrange
        var metrics = CreateTestMetrics();
        metrics.RecordRevenue(1000m);

        // Act
        metrics.RecordRefund(100m);
        metrics.RecordRefund(50m);
        metrics.RecordRefund(25m);

        // Assert
        metrics.RefundedAmount.Should().Be(175m);
        metrics.NetRevenue.Should().Be(825m);
    }

    [Fact(DisplayName = "Scenario: Daily revenue tracking")]
    public void Scenario_DailyRevenue_ShouldTrackCorrectly()
    {
        // Arrange
        var metrics = new RevenueMetrics(DateTime.Today, TimeGranularity.Daily);

        // Act - Simulate day's transactions
        for (int i = 0; i < 100; i++)
        {
            metrics.RecordRevenue(50m);
        }

        // Some refunds
        for (int i = 0; i < 5; i++)
        {
            metrics.RecordRefund(50m);
        }

        // Assert
        metrics.TotalRevenue.Should().Be(5000m);
        metrics.RefundedAmount.Should().Be(250m);
        metrics.NetRevenue.Should().Be(4750m);
        metrics.TransactionCount.Should().Be(100);
        metrics.AverageTransactionValue.Should().Be(50m);
    }

    [Fact(DisplayName = "Test all time granularities")]
    public void TimeGranularity_AllValues_ShouldWork()
    {
        // Act & Assert
        var daily = new RevenueMetrics(DateTime.Today, TimeGranularity.Daily);
        daily.Granularity.Should().Be(TimeGranularity.Daily);

        var weekly = new RevenueMetrics(DateTime.Today, TimeGranularity.Weekly);
        weekly.Granularity.Should().Be(TimeGranularity.Weekly);

        var monthly = new RevenueMetrics(DateTime.Today, TimeGranularity.Monthly);
        monthly.Granularity.Should().Be(TimeGranularity.Monthly);

        var yearly = new RevenueMetrics(DateTime.Today, TimeGranularity.Yearly);
        yearly.Granularity.Should().Be(TimeGranularity.Yearly);
    }

    private static RevenueMetrics CreateTestMetrics()
    {
        return new RevenueMetrics(DateTime.Today, TimeGranularity.Daily);
    }
}
