using AutoFixture;
using Bogus;
using FluentAssertions;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.UnitTests.Domain;

/// <summary>
/// Unit tests for OrderItem value object
/// Tests creation, validation, and business rules
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class OrderItemTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public OrderItemTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    [Fact(DisplayName = "Create OrderItem: With valid data should succeed")]
    public void CreateOrderItem_WithValidData_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var quantity = 2;
        var priceResult = Money.Create(50.00m, "USD");

        // Act
        var result = OrderItem.Create(productId, productName, quantity, priceResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ProductId.Should().Be(productId);
        result.Value.ProductName.Should().Be(productName);
        result.Value.Quantity.Should().Be(quantity);
        result.Value.UnitPrice.Should().Be(priceResult.Value);
    }

    [Fact(DisplayName = "OrderItem Total Price: Should be calculated correctly")]
    public void OrderItem_TotalPrice_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var quantity = 3;
        var priceResult = Money.Create(25.50m, "USD");

        // Act
        var result = OrderItem.Create(
            Guid.NewGuid(),
            "Product",
            quantity,
            priceResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPrice.Amount.Should().BeApproximately(76.50m, 0.01m); // 3 * 25.50
    }

    [Fact(DisplayName = "Create OrderItem: With empty product ID should fail")]
    public void CreateOrderItem_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var priceResult = Money.Create(50m, "USD");
        
        // Act
        var result = OrderItem.Create(
            Guid.Empty,
            "Product",
            1,
            priceResult.Value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Product ID");
    }

    [Theory(DisplayName = "Create OrderItem: With invalid quantity should fail")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CreateOrderItem_WithInvalidQuantity_ShouldFail(int invalidQuantity)
    {
        // Arrange
        var priceResult = Money.Create(50m, "USD");
        
        // Act
        var result = OrderItem.Create(
            Guid.NewGuid(),
            "Product",
            invalidQuantity,
            priceResult.Value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Quantity");
    }

    [Fact(DisplayName = "Create OrderItem: With empty product name should fail")]
    public void CreateOrderItem_WithEmptyProductName_ShouldFail()
    {
        // Arrange
        var priceResult = Money.Create(50m, "USD");
        
        // Act
        var result = OrderItem.Create(
            Guid.NewGuid(),
            "",
            1,
            priceResult.Value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Product name");
    }

    [Fact(DisplayName = "Create OrderItem: With null product name should fail")]
    public void CreateOrderItem_WithNullProductName_ShouldFail()
    {
        // Arrange
        var priceResult = Money.Create(50m, "USD");
        
        // Act
        var result = OrderItem.Create(
            Guid.NewGuid(),
            null!,
            1,
            priceResult.Value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Product name");
    }

    [Fact(DisplayName = "OrderItem Equality: Same values should be equal")]
    public void OrderItem_Equality_SameValuesShouldBeEqual()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Product";
        var quantity = 2;
        var priceResult = Money.Create(50m, "USD");

        // Act
        var result1 = OrderItem.Create(productId, productName, quantity, priceResult.Value);
        var result2 = OrderItem.Create(productId, productName, quantity, priceResult.Value);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value);
        (result1.Value == result2.Value).Should().BeTrue();
    }

    [Fact(DisplayName = "OrderItem Equality: Different values should not be equal")]
    public void OrderItem_Equality_DifferentValuesShouldNotBeEqual()
    {
        // Arrange
        var price1 = Money.Create(50m, "USD").Value;
        var price2 = Money.Create(100m, "USD").Value;
        
        var result1 = OrderItem.Create(Guid.NewGuid(), "Product1", 1, price1);
        var result2 = OrderItem.Create(Guid.NewGuid(), "Product2", 2, price2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
        (result1.Value == result2.Value).Should().BeFalse();
    }

    [Theory(DisplayName = "OrderItem Total: Various quantities and prices")]
    [InlineData(1, 10.00, 10.00)]
    [InlineData(2, 10.00, 20.00)]
    [InlineData(5, 15.50, 77.50)]
    [InlineData(10, 9.99, 99.90)]
    [InlineData(100, 1.25, 125.00)]
    public void OrderItem_Total_VariousQuantitiesAndPrices(
        int quantity, 
        decimal unitPrice, 
        decimal expectedTotal)
    {
        // Arrange
        var priceResult = Money.Create(unitPrice, "USD");
        
        // Act
        var result = OrderItem.Create(
            Guid.NewGuid(),
            "Product",
            quantity,
            priceResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPrice.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }
}
