using Analytics.Domain.Aggregates;

namespace Analytics.UnitTests.Domain;

/// <summary>
/// Unit tests for OrderMetrics aggregate
/// Tests metric tracking, calculations, and state management
/// </summary>
public class OrderMetricsTests
{
    #region Constructor Tests

    [Fact(DisplayName = "Constructor: Should create with valid metric date")]
    public void Constructor_WithValidDate_ShouldCreateOrderMetrics()
    {
        // Arrange
        var metricDate = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        var metrics = new OrderMetrics(metricDate);

        // Assert
        metrics.Should().NotBeNull();
        metrics.Id.Should().NotBeEmpty();
        metrics.MetricDate.Should().Be(metricDate.Date); // Should normalize to date only
        metrics.TotalOrders.Should().Be(0);
        metrics.CompletedOrders.Should().Be(0);
        metrics.CancelledOrders.Should().Be(0);
        metrics.PendingOrders.Should().Be(0);
        metrics.TotalRevenue.Should().Be(0);
        metrics.AverageOrderValue.Should().Be(0);
        metrics.Currency.Should().Be("USD");
        metrics.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metrics.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Constructor: Should normalize metric date to date only")]
    public void Constructor_WithDateTime_ShouldNormalizeToDateOnly()
    {
        // Arrange
        var metricDate = new DateTime(2024, 1, 15, 10, 30, 45);

        // Act
        var metrics = new OrderMetrics(metricDate);

        // Assert
        metrics.MetricDate.Should().Be(new DateTime(2024, 1, 15));
        metrics.MetricDate.Hour.Should().Be(0);
        metrics.MetricDate.Minute.Should().Be(0);
        metrics.MetricDate.Second.Should().Be(0);
    }

    #endregion

    #region IncrementTotalOrders Tests

    [Fact(DisplayName = "IncrementTotalOrders: Should increment count and revenue")]
    public void IncrementTotalOrders_WithValidValue_ShouldIncrementCountAndRevenue()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);
        var orderValue = 100.50m;

        // Act
        metrics.IncrementTotalOrders(orderValue);

        // Assert
        metrics.TotalOrders.Should().Be(1);
        metrics.TotalRevenue.Should().Be(orderValue);
        metrics.AverageOrderValue.Should().Be(orderValue);
    }

    [Fact(DisplayName = "IncrementTotalOrders: Should calculate correct average")]
    public void IncrementTotalOrders_MultipleOrders_ShouldCalculateCorrectAverage()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementTotalOrders(100m);
        metrics.IncrementTotalOrders(200m);
        metrics.IncrementTotalOrders(150m);

        // Assert
        metrics.TotalOrders.Should().Be(3);
        metrics.TotalRevenue.Should().Be(450m);
        metrics.AverageOrderValue.Should().Be(150m);
    }

    [Fact(DisplayName = "IncrementTotalOrders: Zero value should work")]
    public void IncrementTotalOrders_WithZeroValue_ShouldWork()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementTotalOrders(0);

        // Assert
        metrics.TotalOrders.Should().Be(1);
        metrics.TotalRevenue.Should().Be(0);
        metrics.AverageOrderValue.Should().Be(0);
    }

    [Fact(DisplayName = "IncrementTotalOrders: Should update UpdatedAt timestamp")]
    public void IncrementTotalOrders_ShouldUpdateTimestamp()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);
        var originalUpdatedAt = metrics.UpdatedAt;
        Thread.Sleep(10);

        // Act
        metrics.IncrementTotalOrders(100m);

        // Assert
        metrics.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region IncrementCompletedOrders Tests

    [Fact(DisplayName = "IncrementCompletedOrders: Should increment count")]
    public void IncrementCompletedOrders_ShouldIncrementCount()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementCompletedOrders();

        // Assert
        metrics.CompletedOrders.Should().Be(1);
    }

    [Fact(DisplayName = "IncrementCompletedOrders: Multiple increments should work")]
    public void IncrementCompletedOrders_MultipleIncrements_ShouldAccumulate()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementCompletedOrders();
        metrics.IncrementCompletedOrders();
        metrics.IncrementCompletedOrders();

        // Assert
        metrics.CompletedOrders.Should().Be(3);
    }

    #endregion

    #region IncrementCancelledOrders Tests

    [Fact(DisplayName = "IncrementCancelledOrders: Should increment count")]
    public void IncrementCancelledOrders_ShouldIncrementCount()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementCancelledOrders();

        // Assert
        metrics.CancelledOrders.Should().Be(1);
    }

    [Fact(DisplayName = "IncrementCancelledOrders: Multiple increments should work")]
    public void IncrementCancelledOrders_MultipleIncrements_ShouldAccumulate()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementCancelledOrders();
        metrics.IncrementCancelledOrders();

        // Assert
        metrics.CancelledOrders.Should().Be(2);
    }

    #endregion

    #region IncrementPendingOrders Tests

    [Fact(DisplayName = "IncrementPendingOrders: Should increment count")]
    public void IncrementPendingOrders_ShouldIncrementCount()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.IncrementPendingOrders();

        // Assert
        metrics.PendingOrders.Should().Be(1);
    }

    #endregion

    #region DecrementPendingOrders Tests

    [Fact(DisplayName = "DecrementPendingOrders: Should decrement count")]
    public void DecrementPendingOrders_WithPositiveCount_ShouldDecrement()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);
        metrics.IncrementPendingOrders();
        metrics.IncrementPendingOrders();

        // Act
        metrics.DecrementPendingOrders();

        // Assert
        metrics.PendingOrders.Should().Be(1);
    }

    [Fact(DisplayName = "DecrementPendingOrders: Should not go below zero")]
    public void DecrementPendingOrders_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.DecrementPendingOrders();

        // Assert
        metrics.PendingOrders.Should().Be(0);
    }

    [Fact(DisplayName = "DecrementPendingOrders: Multiple decrements should not go negative")]
    public void DecrementPendingOrders_MultipleAtZero_ShouldStayAtZero()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act
        metrics.DecrementPendingOrders();
        metrics.DecrementPendingOrders();
        metrics.DecrementPendingOrders();

        // Assert
        metrics.PendingOrders.Should().Be(0);
    }

    #endregion

    #region Integration Scenarios

    [Fact(DisplayName = "Scenario: Complete order lifecycle should update metrics correctly")]
    public void Scenario_CompleteOrderLifecycle_ShouldUpdateMetrics()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act - Simulate order lifecycle
        metrics.IncrementTotalOrders(150m);
        metrics.IncrementPendingOrders();
        
        metrics.IncrementTotalOrders(200m);
        metrics.IncrementPendingOrders();
        
        // First order completes
        metrics.DecrementPendingOrders();
        metrics.IncrementCompletedOrders();
        
        // Second order cancelled
        metrics.DecrementPendingOrders();
        metrics.IncrementCancelledOrders();

        // Assert
        metrics.TotalOrders.Should().Be(2);
        metrics.PendingOrders.Should().Be(0);
        metrics.CompletedOrders.Should().Be(1);
        metrics.CancelledOrders.Should().Be(1);
        metrics.TotalRevenue.Should().Be(350m);
        metrics.AverageOrderValue.Should().Be(175m);
    }

    [Fact(DisplayName = "Scenario: Daily metrics aggregation")]
    public void Scenario_DailyMetricsAggregation_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new OrderMetrics(new DateTime(2024, 1, 15));

        // Act - Simulate day's orders
        for (int i = 0; i < 10; i++)
        {
            metrics.IncrementTotalOrders(100m + (i * 10));
        }

        for (int i = 0; i < 7; i++)
        {
            metrics.IncrementCompletedOrders();
        }

        for (int i = 0; i < 2; i++)
        {
            metrics.IncrementCancelledOrders();
        }

        metrics.IncrementPendingOrders();

        // Assert
        metrics.TotalOrders.Should().Be(10);
        metrics.CompletedOrders.Should().Be(7);
        metrics.CancelledOrders.Should().Be(2);
        metrics.PendingOrders.Should().Be(1);
        metrics.TotalRevenue.Should().Be(1450m); // 100+110+120+...+190
        metrics.AverageOrderValue.Should().Be(145m);
    }

    [Fact(DisplayName = "Scenario: High volume day")]
    public void Scenario_HighVolumeDay_ShouldHandleCorrectly()
    {
        // Arrange
        var metrics = new OrderMetrics(DateTime.Today);

        // Act - Simulate 1000 orders
        for (int i = 1; i <= 1000; i++)
        {
            metrics.IncrementTotalOrders(50m);
            metrics.IncrementCompletedOrders();
        }

        // Assert
        metrics.TotalOrders.Should().Be(1000);
        metrics.CompletedOrders.Should().Be(1000);
        metrics.TotalRevenue.Should().Be(50000m);
        metrics.AverageOrderValue.Should().Be(50m);
    }

    #endregion
}
