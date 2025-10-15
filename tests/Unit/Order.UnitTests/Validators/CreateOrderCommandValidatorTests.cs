using FluentAssertions;
using FluentValidation.TestHelper;
using Order.Application.Commands;
using Order.Application.Validators;
using Xunit;

namespace Order.UnitTests.Validators;

/// <summary>
/// Unit tests for CreateOrderCommandValidator
/// Tests all validation rules for order creation
/// </summary>
public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderCommandValidatorTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.Empty,
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required");
    }

    [Fact]
    public void Validate_EmptyStreet_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Street)
            .WithErrorMessage("Street is required");
    }

    [Fact]
    public void Validate_TooLongStreet_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: new string('A', 201), // 201 characters
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Street);
    }

    [Fact]
    public void Validate_EmptyCity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City is required");
    }

    [Fact]
    public void Validate_TooLongCity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: new string('A', 101), // 101 characters
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_EmptyPostalCode_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code is required");
    }

    [Fact]
    public void Validate_TooLongPostalCode_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: new string('1', 21), // 21 characters
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostalCode);
    }

    [Fact]
    public void Validate_EmptyCountry_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country is required");
    }

    [Fact]
    public void Validate_TooLongCountry_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: new string('A', 101), // 101 characters
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public void Validate_EmptyItems_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order must have at least one item");
    }

    [Fact]
    public void Validate_ItemWithEmptyProductId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.Empty, "Test Product", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("Product ID is required");
    }

    [Fact]
    public void Validate_ItemWithEmptyProductName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "", 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ProductName")
            .WithErrorMessage("Product name is required");
    }

    [Fact]
    public void Validate_ItemWithTooLongProductName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), new string('A', 201), 1, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ProductName");
    }

    [Fact]
    public void Validate_ItemWithZeroQuantity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 0, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity must be positive");
    }

    [Fact]
    public void Validate_ItemWithNegativeQuantity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", -5, 29.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity must be positive");
    }

    [Fact]
    public void Validate_ItemWithZeroPrice_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 0m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
            .WithErrorMessage("Unit price must be positive");
    }

    [Fact]
    public void Validate_ItemWithNegativePrice_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, -10.99m, "USD")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
            .WithErrorMessage("Unit price must be positive");
    }

    [Fact]
    public void Validate_ItemWithEmptyCurrency_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Currency")
            .WithErrorMessage("Currency is required");
    }

    [Fact]
    public void Validate_ItemWithInvalidCurrencyLength_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 29.99m, "US") // Invalid: 2 characters
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Currency")
            .WithErrorMessage("Currency must be 3 characters (ISO 4217)");
    }

    [Fact]
    public void Validate_MultipleItems_AllValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product 1", 1, 10.00m, "USD"),
                new(Guid.NewGuid(), "Product 2", 2, 20.00m, "USD"),
                new(Guid.NewGuid(), "Product 3", 3, 30.00m, "EUR")
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MultipleItemsWithSomeInvalid_ShouldHaveValidationErrorsForInvalidItems()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Main St",
            City: "San Francisco",
            State: "CA",
            PostalCode: "94102",
            Country: "USA",
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Valid Product", 1, 10.00m, "USD"),
                new(Guid.Empty, "", 0, -5.00m, "US") // All invalid
            });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[1].ProductId");
        result.ShouldHaveValidationErrorFor("Items[1].ProductName");
        result.ShouldHaveValidationErrorFor("Items[1].Quantity");
        result.ShouldHaveValidationErrorFor("Items[1].UnitPrice");
        result.ShouldHaveValidationErrorFor("Items[1].Currency");
    }
}
