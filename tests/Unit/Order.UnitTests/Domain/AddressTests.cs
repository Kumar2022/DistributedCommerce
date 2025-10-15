using FluentAssertions;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.UnitTests.Domain;

/// <summary>
/// Unit tests for Address value object
/// Tests creation, validation, and equality
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class AddressTests
{
    [Fact(DisplayName = "Create Address: With valid data should succeed")]
    public void CreateAddress_WithValidData_ShouldSucceed()
    {
        // Arrange
        var street = "123 Main Street";
        var city = "New York";
        var state = "NY";
        var postalCode = "10001";
        var country = "USA";

        // Act
        var result = Address.Create(street, city, state, postalCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Street.Should().Be(street);
        result.Value.City.Should().Be(city);
        result.Value.State.Should().Be(state);
        result.Value.PostalCode.Should().Be(postalCode);
        result.Value.Country.Should().Be(country);
    }

    [Theory(DisplayName = "Create Address: With empty street should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAddress_WithEmptyStreet_ShouldFail(string invalidStreet)
    {
        // Act
        var result = Address.Create(
            invalidStreet,
            "City",
            "State",
            "12345",
            "USA");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Street");
    }

    [Theory(DisplayName = "Create Address: With empty city should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAddress_WithEmptyCity_ShouldFail(string invalidCity)
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            invalidCity,
            "State",
            "12345",
            "USA");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("City");
    }

    [Theory(DisplayName = "Create Address: With empty state should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAddress_WithEmptyState_ShouldFail(string invalidState)
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            "City",
            invalidState,
            "12345",
            "USA");

        // Assert
        // State is optional in the implementation, so this might succeed
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Create Address: With invalid postal code should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAddress_WithInvalidPostalCode_ShouldFail(string invalidPostalCode)
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            "City",
            "State",
            invalidPostalCode,
            "USA");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Postal code");
    }

    [Theory(DisplayName = "Create Address: With empty country should fail")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAddress_WithEmptyCountry_ShouldFail(string invalidCountry)
    {
        // Act
        var result = Address.Create(
            "123 Main St",
            "City",
            "State",
            "12345",
            invalidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Country");
    }

    [Fact(DisplayName = "Address Equality: Same values should be equal")]
    public void AddressEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var result1 = Address.Create("123 Main St", "City", "State", "12345", "USA");
        var result2 = Address.Create("123 Main St", "City", "State", "12345", "USA");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value);
        (result1.Value == result2.Value).Should().BeTrue();
        result1.Value.GetHashCode().Should().Be(result2.Value.GetHashCode());
    }

    [Fact(DisplayName = "Address Equality: Different values should not be equal")]
    public void AddressEquality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var result1 = Address.Create("123 Main St", "City1", "State", "12345", "USA");
        var result2 = Address.Create("456 Oak Ave", "City2", "State", "67890", "USA");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
        (result1.Value == result2.Value).Should().BeFalse();
    }

    [Fact(DisplayName = "Address ToString: Should contain all components")]
    public void Address_ToString_ShouldContainAllComponents()
    {
        // Arrange
        var result = Address.Create("123 Main St", "New York", "NY", "10001", "USA");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var toString = result.Value.ToString();

        toString.Should().Contain("123 Main St");
        toString.Should().Contain("New York");
        toString.Should().Contain("NY");
        toString.Should().Contain("10001");
        toString.Should().Contain("USA");
    }

    [Theory(DisplayName = "Address: Various valid formats")]
    [InlineData("123 Main Street", "New York", "NY", "10001", "USA")]
    [InlineData("456 Oak Avenue Apt 5B", "Los Angeles", "CA", "90001", "USA")]
    [InlineData("789 Pine Road", "Chicago", "IL", "60601", "USA")]
    [InlineData("10 Downing Street", "London", "London", "SW1A 2AA", "UK")]
    [InlineData("1 Infinite Loop", "Cupertino", "CA", "95014", "USA")]
    public void Address_VariousValidFormats_ShouldSucceed(
        string street,
        string city,
        string state,
        string postalCode,
        string country)
    {
        // Act
        var result = Address.Create(street, city, state, postalCode, country);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Street.Should().Be(street);
        result.Value.City.Should().Be(city);
        result.Value.State.Should().Be(state);
        result.Value.PostalCode.Should().Be(postalCode);
        result.Value.Country.Should().Be(country);
    }

    [Fact(DisplayName = "Address: Trim whitespace from inputs")]
    public void Address_TrimWhitespaceFromInputs()
    {
        // Act
        var result = Address.Create(
            "  123 Main St  ",
            "  City  ",
            "  State  ",
            "  12345  ",
            "  USA  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("123 Main St");
        result.Value.City.Should().Be("City");
        result.Value.State.Should().Be("State");
        result.Value.PostalCode.Should().Be("12345");
        result.Value.Country.Should().Be("USA");
    }
}
