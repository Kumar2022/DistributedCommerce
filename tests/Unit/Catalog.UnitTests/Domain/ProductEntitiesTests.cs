using AutoFixture;
using Bogus;
using Catalog.Domain.Aggregates;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.Domain;

/// <summary>
/// Unit tests for Product entities (ProductImage and ProductAttribute)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class ProductEntitiesTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public ProductEntitiesTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region ProductImage Tests

    [Fact(DisplayName = "ProductImage: Create with valid data should succeed")]
    public void ProductImage_CreateWithValidData_ShouldSucceed()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var altText = "Product Image";
        var displayOrder = 1;
        var isPrimary = true;

        // Act
        var image = ProductImage.Create(url, altText, displayOrder, isPrimary);

        // Assert
        image.Should().NotBeNull();
        image.Id.Should().NotBe(Guid.Empty);
        image.Url.Should().Be(url);
        image.AltText.Should().Be(altText);
        image.DisplayOrder.Should().Be(displayOrder);
        image.IsPrimary.Should().Be(isPrimary);
    }

    [Fact(DisplayName = "ProductImage: Create with defaults should succeed")]
    public void ProductImage_CreateWithDefaults_ShouldSucceed()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var altText = "Product Image";

        // Act
        var image = ProductImage.Create(url, altText);

        // Assert
        image.DisplayOrder.Should().Be(0);
        image.IsPrimary.Should().BeFalse();
    }

    [Theory(DisplayName = "ProductImage: Create with empty URL should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductImage_CreateWithEmptyUrl_ShouldThrow(string url)
    {
        // Arrange & Act
        var act = () => ProductImage.Create(url, "Alt Text");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Image URL is required*");
    }

    [Fact(DisplayName = "ProductImage: Set as primary should update flag")]
    public void ProductImage_SetAsPrimary_ShouldUpdateFlag()
    {
        // Arrange
        var image = ProductImage.Create("https://example.com/image.jpg", "Alt Text", 0, false);
        image.IsPrimary.Should().BeFalse();

        // Act
        image.SetAsPrimary(true);

        // Assert
        image.IsPrimary.Should().BeTrue();
    }

    [Fact(DisplayName = "ProductImage: Update display order should change order")]
    public void ProductImage_UpdateDisplayOrder_ShouldChangeOrder()
    {
        // Arrange
        var image = ProductImage.Create("https://example.com/image.jpg", "Alt Text", 0, false);
        var newDisplayOrder = 5;

        // Act
        image.UpdateDisplayOrder(newDisplayOrder);

        // Assert
        image.DisplayOrder.Should().Be(newDisplayOrder);
    }

    #endregion

    #region ProductAttribute Tests

    [Fact(DisplayName = "ProductAttribute: Create with valid data should succeed")]
    public void ProductAttribute_CreateWithValidData_ShouldSucceed()
    {
        // Arrange
        var key = "color";
        var value = "red";
        var displayName = "Color";

        // Act
        var attribute = ProductAttribute.Create(key, value, displayName);

        // Assert
        attribute.Should().NotBeNull();
        attribute.Id.Should().NotBe(Guid.Empty);
        attribute.Key.Should().Be(key);
        attribute.Value.Should().Be(value);
        attribute.DisplayName.Should().Be(displayName);
    }

    [Fact(DisplayName = "ProductAttribute: Create with null display name should use key")]
    public void ProductAttribute_CreateWithNullDisplayName_ShouldUseKey()
    {
        // Arrange
        var key = "size";
        var value = "large";

        // Act
        var attribute = ProductAttribute.Create(key, value, null);

        // Assert
        attribute.DisplayName.Should().Be(key);
    }

    [Theory(DisplayName = "ProductAttribute: Create with empty key should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductAttribute_CreateWithEmptyKey_ShouldThrow(string key)
    {
        // Arrange & Act
        var act = () => ProductAttribute.Create(key, "value", "Display Name");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Attribute key is required*");
    }

    [Fact(DisplayName = "ProductAttribute: Update value should change value")]
    public void ProductAttribute_UpdateValue_ShouldChangeValue()
    {
        // Arrange
        var attribute = ProductAttribute.Create("color", "red", "Color");
        var newValue = "blue";

        // Act
        attribute.UpdateValue(newValue);

        // Assert
        attribute.Value.Should().Be(newValue);
    }

    #endregion
}
