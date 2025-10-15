using Inventory.Domain.Aggregates;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Comprehensive unit tests for Inventory Product aggregate
/// Tests stock management, reservations, and concurrency
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class ProductTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public ProductTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Product Creation Tests

    [Fact(DisplayName = "Create Product: With valid data should succeed")]
    public void CreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange
        var sku = "TEST-SKU-001";
        var name = "Test Product";
        var initialStock = 100;
        var reorderLevel = 10;
        var reorderQuantity = 50;

        // Act
        var result = Product.Create(sku, name, initialStock, reorderLevel, reorderQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var product = result.Value;
        product.Should().NotBeNull();
        product.Id.Should().NotBe(Guid.Empty);
        product.Sku.Should().Be(sku);
        product.Name.Should().Be(name);
        product.StockQuantity.Should().Be(initialStock);
        product.ReservedQuantity.Should().Be(0);
        product.AvailableQuantity.Should().Be(initialStock);
        product.ReorderLevel.Should().Be(reorderLevel);
        product.ReorderQuantity.Should().Be(reorderQuantity);
    }

    [Theory(DisplayName = "Create Product: With various stock levels")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void CreateProduct_WithVariousStockLevels_ShouldSucceed(int stock)
    {
        // Arrange & Act
        var result = Product.Create("SKU-001", "Product", stock);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StockQuantity.Should().Be(stock);
    }

    [Theory(DisplayName = "Create Product: With empty SKU should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProduct_WithEmptySku_ShouldFail(string sku)
    {
        // Arrange & Act
        var result = Product.Create(sku, "Product", 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Theory(DisplayName = "Create Product: With empty name should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProduct_WithEmptyName_ShouldFail(string name)
    {
        // Arrange & Act
        var result = Product.Create("SKU-001", name, 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Create Product: With negative stock should fail")]
    public void CreateProduct_WithNegativeStock_ShouldFail()
    {
        // Arrange & Act
        var result = Product.Create("SKU-001", "Product", -10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Create Product: With negative reorder level should fail")]
    public void CreateProduct_WithNegativeReorderLevel_ShouldFail()
    {
        // Arrange & Act
        var result = Product.Create("SKU-001", "Product", 100, -5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Theory(DisplayName = "Create Product: With invalid reorder quantity should fail")]
    [InlineData(0)]
    [InlineData(-10)]
    public void CreateProduct_WithInvalidReorderQuantity_ShouldFail(int reorderQuantity)
    {
        // Arrange & Act
        var result = Product.Create("SKU-001", "Product", 100, 10, reorderQuantity);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    #endregion

    #region Stock Reservation Tests

    [Fact(DisplayName = "Reserve Stock: With sufficient stock should succeed")]
    public void ReserveStock_WithSufficientStock_ShouldSucceed()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        var quantity = 10;

        // Act
        var result = product.ReserveStock(orderId, quantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReservedQuantity.Should().Be(quantity);
        product.AvailableQuantity.Should().Be(90);
        product.Reservations.Should().HaveCount(1);
        product.Reservations.First().OrderId.Should().Be(orderId);
        product.Reservations.First().Quantity.Should().Be(quantity);
        product.Reservations.First().Status.Should().Be(ReservationStatus.Active);
    }

    [Fact(DisplayName = "Reserve Stock: Multiple reservations should accumulate")]
    public void ReserveStock_MultipleReservations_ShouldAccumulate()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var order1 = Guid.NewGuid();
        var order2 = Guid.NewGuid();

        // Act
        var result1 = product.ReserveStock(order1, 10);
        var result2 = product.ReserveStock(order2, 20);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        product.ReservedQuantity.Should().Be(30);
        product.AvailableQuantity.Should().Be(70);
        product.Reservations.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Reserve Stock: With insufficient stock should fail")]
    public void ReserveStock_WithInsufficientStock_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(10);
        var orderId = Guid.NewGuid();

        // Act
        var result = product.ReserveStock(orderId, 20);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Insufficient stock");
        product.ReservedQuantity.Should().Be(0);
    }

    [Theory(DisplayName = "Reserve Stock: With invalid quantity should fail")]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReserveStock_WithInvalidQuantity_ShouldFail(int quantity)
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();

        // Act
        var result = product.ReserveStock(orderId, quantity);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Reserve Stock: With custom expiration time should set correctly")]
    public void ReserveStock_WithCustomExpiration_ShouldSetCorrectly()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        var expirationTime = TimeSpan.FromMinutes(30);

        // Act
        var result = product.ReserveStock(orderId, 10, expirationTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var reservation = product.Reservations.First();
        var expectedExpiration = DateTime.UtcNow.Add(expirationTime);
        reservation.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Confirm Reservation Tests

    [Fact(DisplayName = "Confirm Reservation: Active reservation should be confirmed")]
    public void ConfirmReservation_ActiveReservation_ShouldBeConfirmed()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        product.ReserveStock(orderId, 10);
        var originalStock = product.StockQuantity;

        // Act
        var result = product.ConfirmReservation(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(originalStock - 10);
        product.ReservedQuantity.Should().Be(0);
        product.AvailableQuantity.Should().Be(90);
        product.Reservations.First().Status.Should().Be(ReservationStatus.Confirmed);
        product.Reservations.First().ConfirmedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Confirm Reservation: Non-existent reservation should fail")]
    public void ConfirmReservation_NonExistentReservation_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();

        // Act
        var result = product.ConfirmReservation(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("No active reservation found");
    }

    [Fact(DisplayName = "Confirm Reservation: Already confirmed should fail")]
    public void ConfirmReservation_AlreadyConfirmed_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        product.ReserveStock(orderId, 10);
        product.ConfirmReservation(orderId);

        // Act
        var result = product.ConfirmReservation(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Release Reservation Tests

    [Fact(DisplayName = "Release Reservation: Active reservation should be released")]
    public void ReleaseReservation_ActiveReservation_ShouldBeReleased()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        product.ReserveStock(orderId, 10);

        // Act
        var result = product.ReleaseReservation(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReservedQuantity.Should().Be(0);
        product.AvailableQuantity.Should().Be(100);
        product.Reservations.First().Status.Should().Be(ReservationStatus.Released);
        product.Reservations.First().ReleasedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Release Reservation: Non-existent reservation should fail")]
    public void ReleaseReservation_NonExistentReservation_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();

        // Act
        var result = product.ReleaseReservation(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("No active reservation found");
    }

    #endregion

    #region Stock Adjustment Tests

    [Fact(DisplayName = "Adjust Stock: Positive adjustment should increase stock")]
    public void AdjustStock_PositiveAdjustment_ShouldIncreaseStock()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var adjustment = 50;
        var reason = "Restock from warehouse";

        // Act
        var result = product.AdjustStock(adjustment, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(150);
        product.LastRestockDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Adjust Stock: Negative adjustment should decrease stock")]
    public void AdjustStock_NegativeAdjustment_ShouldDecreaseStock()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var adjustment = -20;
        var reason = "Damaged goods";

        // Act
        var result = product.AdjustStock(adjustment, reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(80);
    }

    [Fact(DisplayName = "Adjust Stock: Resulting in negative stock should fail")]
    public void AdjustStock_ResultingInNegativeStock_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(50);
        var adjustment = -100;
        var reason = "Test";

        // Act
        var result = product.AdjustStock(adjustment, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Stock cannot be negative");
        product.StockQuantity.Should().Be(50); // Should not change
    }

    [Theory(DisplayName = "Adjust Stock: Without reason should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AdjustStock_WithoutReason_ShouldFail(string reason)
    {
        // Arrange
        var product = CreateValidProduct(100);

        // Act
        var result = product.AdjustStock(10, reason);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("reason is required");
    }

    #endregion

    #region Expired Reservations Tests

    [Fact(DisplayName = "Release Expired Reservations: Should release expired ones")]
    public void ReleaseExpiredReservations_ShouldReleaseExpiredOnes()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var order1 = Guid.NewGuid();
        var order2 = Guid.NewGuid();
        
        // Create one that will expire immediately
        product.ReserveStock(order1, 10, TimeSpan.FromMilliseconds(-1));
        // Create one that won't expire
        product.ReserveStock(order2, 20, TimeSpan.FromMinutes(10));
        
        // Give time for first reservation to expire
        System.Threading.Thread.Sleep(100);

        // Act
        var result = product.ReleaseExpiredReservations();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReservedQuantity.Should().Be(20); // Only second reservation remains
        product.AvailableQuantity.Should().Be(80);
        
        var expiredReservation = product.Reservations.First(r => r.OrderId == order1);
        expiredReservation.Status.Should().Be(ReservationStatus.Expired);
        
        var activeReservation = product.Reservations.First(r => r.OrderId == order2);
        activeReservation.Status.Should().Be(ReservationStatus.Active);
    }

    [Fact(DisplayName = "Release Expired Reservations: With no expired should succeed")]
    public void ReleaseExpiredReservations_WithNoExpired_ShouldSucceed()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var orderId = Guid.NewGuid();
        product.ReserveStock(orderId, 10, TimeSpan.FromMinutes(10));

        // Act
        var result = product.ReleaseExpiredReservations();

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReservedQuantity.Should().Be(10); // Should remain unchanged
        product.Reservations.First().Status.Should().Be(ReservationStatus.Active);
    }

    #endregion

    #region Reorder Settings Tests

    [Fact(DisplayName = "Update Reorder Settings: With valid values should succeed")]
    public void UpdateReorderSettings_WithValidValues_ShouldSucceed()
    {
        // Arrange
        var product = CreateValidProduct(100);
        var newReorderLevel = 20;
        var newReorderQuantity = 100;

        // Act
        var result = product.UpdateReorderSettings(newReorderLevel, newReorderQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReorderLevel.Should().Be(newReorderLevel);
        product.ReorderQuantity.Should().Be(newReorderQuantity);
    }

    [Fact(DisplayName = "Update Reorder Settings: With negative level should fail")]
    public void UpdateReorderSettings_WithNegativeLevel_ShouldFail()
    {
        // Arrange
        var product = CreateValidProduct(100);

        // Act
        var result = product.UpdateReorderSettings(-5, 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    [Theory(DisplayName = "Update Reorder Settings: With invalid quantity should fail")]
    [InlineData(0)]
    [InlineData(-10)]
    public void UpdateReorderSettings_WithInvalidQuantity_ShouldFail(int quantity)
    {
        // Arrange
        var product = CreateValidProduct(100);

        // Act
        var result = product.UpdateReorderSettings(10, quantity);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Validation");
    }

    #endregion

    #region Helper Methods

    private Product CreateValidProduct(int initialStock = 100)
    {
        var result = Product.Create("TEST-SKU", "Test Product", initialStock, 10, 50);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    #endregion
}
