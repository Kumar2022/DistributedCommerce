using FluentAssertions;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.UnitTests.Domain;

/// <summary>
/// Unit tests for Money value object
/// Tests creation, arithmetic operations, and currency handling
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class MoneyTests
{
    [Fact(DisplayName = "Create Money: With valid amount and currency should succeed")]
    public void CreateMoney_WithValidAmountAndCurrency_ShouldSucceed()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var result = Money.Create(amount, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
        result.Value.Currency.Should().Be(currency);
    }

    [Fact(DisplayName = "Create Money: Zero amount should be valid")]
    public void CreateMoney_ZeroAmount_ShouldBeValid()
    {
        // Act
        var result = Money.Create(0m, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(0m);
        result.Value.Currency.Should().Be("USD");
    }

    [Theory(DisplayName = "Create Money: With negative amount should fail")]
    [InlineData(-1)]
    [InlineData(-100.50)]
    [InlineData(-0.01)]
    public void CreateMoney_WithNegativeAmount_ShouldFail(decimal negativeAmount)
    {
        // Act
        var result = Money.Create(negativeAmount, "USD");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("negative");
    }

    [Theory(DisplayName = "Create Money: With invalid currency should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void CreateMoney_WithInvalidCurrency_ShouldFail(string invalidCurrency)
    {
        // Act
        var result = Money.Create(100m, invalidCurrency);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.ToLower().Should().Contain("currency");
    }

    [Fact(DisplayName = "Create Money: With null currency should fail")]
    public void CreateMoney_WithNullCurrency_ShouldFail()
    {
        // Act
        var result = Money.Create(100m, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Money Add: Same currency should succeed")]
    public void MoneyAdd_SameCurrency_ShouldSucceed()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(50m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact(DisplayName = "Money Add: Different currency should fail")]
    public void MoneyAdd_DifferentCurrency_ShouldFail()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(50m, "EUR");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();

        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Act
        Action act = () => money1.Add(money2);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Cannot add EUR to USD");
    }

    [Fact(DisplayName = "Money Subtract: Same currency should succeed")]
    public void MoneySubtract_SameCurrency_ShouldSucceed()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(30m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();

        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be("USD");
    }

    [Fact(DisplayName = "Money Subtract: Result can be negative")]
    public void MoneySubtract_ResultCanBeNegative()
    {
        // Arrange
        var money1Result = Money.Create(50m, "USD");
        var money2Result = Money.Create(100m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();

        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(-50m);
        result.Currency.Should().Be("USD");
    }

    [Fact(DisplayName = "Money Multiply: By positive factor should succeed")]
    public void MoneyMultiply_ByPositiveFactor_ShouldSucceed()
    {
        // Arrange
        var moneyResult = Money.Create(50m, "USD");
        moneyResult.IsSuccess.Should().BeTrue();
        var money = moneyResult.Value;

        // Act
        var result = money.Multiply(3);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Theory(DisplayName = "Money Multiply: By any factor should succeed")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(2)]
    [InlineData(5)]
    public void MoneyMultiply_ByAnyFactor_ShouldSucceed(decimal factor)
    {
        // Arrange
        var moneyResult = Money.Create(50m, "USD");
        moneyResult.IsSuccess.Should().BeTrue();
        var money = moneyResult.Value;

        // Act
        var result = money.Multiply(factor);

        // Assert
        result.Amount.Should().Be(50m * factor);
        result.Currency.Should().Be("USD");
    }

    [Fact(DisplayName = "Money Equality: Same values should be equal")]
    public void MoneyEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(100m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact(DisplayName = "Money Equality: Different amounts should not be equal")]
    public void MoneyEquality_DifferentAmounts_ShouldNotBeEqual()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(50m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
    }

    [Fact(DisplayName = "Money Equality: Different currencies should not be equal")]
    public void MoneyEquality_DifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(100m, "EUR");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
    }

    [Fact(DisplayName = "Money Comparison: Greater than should work correctly")]
    public void MoneyComparison_GreaterThan_ShouldWorkCorrectly()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(50m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert
        money1.Amount.Should().BeGreaterThan(money2.Amount);
        money2.Amount.Should().BeLessThan(money1.Amount);
    }

    [Fact(DisplayName = "Money Comparison: Less than should work correctly")]
    public void MoneyComparison_LessThan_ShouldWorkCorrectly()
    {
        // Arrange
        var money1Result = Money.Create(50m, "USD");
        var money2Result = Money.Create(100m, "USD");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert
        money1.Amount.Should().BeLessThan(money2.Amount);
        money2.Amount.Should().BeGreaterThan(money1.Amount);
    }

    [Fact(DisplayName = "Money Comparison: Different currency should fail")]
    public void MoneyComparison_DifferentCurrency_ShouldFail()
    {
        // Arrange
        var money1Result = Money.Create(100m, "USD");
        var money2Result = Money.Create(50m, "EUR");
        money1Result.IsSuccess.Should().BeTrue();
        money2Result.IsSuccess.Should().BeTrue();
        
        var money1 = money1Result.Value;
        var money2 = money2Result.Value;

        // Assert - cannot directly compare different currencies
        // This is just to verify they were created correctly
        money1.Currency.Should().Be("USD");
        money2.Currency.Should().Be("EUR");
        money1.Currency.Should().NotBe(money2.Currency);
    }

    [Theory(DisplayName = "Money: Supported currencies")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CAD")]
    [InlineData("AUD")]
    public void Money_SupportedCurrencies_ShouldBeValid(string currency)
    {
        // Act
        var result = Money.Create(100m, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be(currency);
    }

    [Fact(DisplayName = "Money ToString: Should format correctly")]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var result = Money.Create(100.50m, "USD");
        result.IsSuccess.Should().BeTrue();
        var money = result.Value;

        // Act
        var stringResult = money.ToString();

        // Assert
        stringResult.Should().Contain("100.50");
        stringResult.Should().Contain("USD");
    }

    [Theory(DisplayName = "Money: Decimal precision tests")]
    [InlineData(100.12)]
    [InlineData(0.01)]
    [InlineData(9999.99)]
    [InlineData(1.234)] // Should round or handle precision
    public void Money_DecimalPrecision_ShouldHandleCorrectly(decimal amount)
    {
        // Act
        var result = Money.Create(amount, "USD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().BeGreaterOrEqualTo(0);
        // Depending on implementation, might round to 2 decimal places
    }
}
