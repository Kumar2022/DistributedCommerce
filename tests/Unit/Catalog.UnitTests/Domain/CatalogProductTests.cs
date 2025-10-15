using AutoFixture;
using Bogus;
using Catalog.Domain.Aggregates;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.Domain;

/// <summary>
/// Comprehensive unit tests for CatalogProduct aggregate root
/// Tests product lifecycle, pricing, inventory, images, attributes, and publishing
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class CatalogProductTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public CatalogProductTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Product Creation Tests

    [Fact(DisplayName = "Create Product: With valid data should succeed")]
    public void CreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var sku = "TEST-SKU-001";
        var categoryId = Guid.NewGuid();
        var brand = "Test Brand";
        var price = 99.99m;

        // Act
        var product = CatalogProduct.Create(name, description, sku, categoryId, brand, price);

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBe(Guid.Empty);
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Sku.Should().Be(sku);
        product.CategoryId.Should().Be(categoryId);
        product.Brand.Should().Be(brand);
        product.Price.Should().Be(price);
        product.Currency.Should().Be("USD");
        product.Status.Should().Be(ProductStatus.Draft);
        product.IsFeatured.Should().BeFalse();
        product.Slug.Should().Be("test-product");
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory(DisplayName = "Create Product: With various prices")]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(100.00)]
    [InlineData(9999.99)]
    public void CreateProduct_WithVariousPrices_ShouldSucceed(decimal price)
    {
        // Arrange
        var name = "Product";
        var description = "Description";
        var sku = "SKU-001";
        var categoryId = Guid.NewGuid();
        var brand = "Brand";

        // Act
        var product = CatalogProduct.Create(name, description, sku, categoryId, brand, price);

        // Assert
        product.Price.Should().Be(price);
    }

    [Theory(DisplayName = "Create Product: With empty name should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProduct_WithEmptyName_ShouldThrow(string name)
    {
        // Arrange & Act
        var act = () => CatalogProduct.Create(
            name, 
            "Description", 
            "SKU-001", 
            Guid.NewGuid(), 
            "Brand", 
            100m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product name is required*");
    }

    [Theory(DisplayName = "Create Product: With empty SKU should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProduct_WithEmptySku_ShouldThrow(string sku)
    {
        // Arrange & Act
        var act = () => CatalogProduct.Create(
            "Product", 
            "Description", 
            sku, 
            Guid.NewGuid(), 
            "Brand", 
            100m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SKU is required*");
    }

    [Fact(DisplayName = "Create Product: With negative price should throw")]
    public void CreateProduct_WithNegativePrice_ShouldThrow()
    {
        // Arrange & Act
        var act = () => CatalogProduct.Create(
            "Product", 
            "Description", 
            "SKU-001", 
            Guid.NewGuid(), 
            "Brand", 
            -10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Price must be positive*");
    }

    [Fact(DisplayName = "Create Product: With custom currency should succeed")]
    public void CreateProduct_WithCustomCurrency_ShouldSucceed()
    {
        // Arrange
        var currency = "EUR";

        // Act
        var product = CatalogProduct.Create(
            "Product", 
            "Description", 
            "SKU-001", 
            Guid.NewGuid(), 
            "Brand", 
            100m, 
            currency);

        // Assert
        product.Currency.Should().Be(currency);
    }

    #endregion

    #region Product Update Tests

    [Fact(DisplayName = "Update Details: Should update name, description, and brand")]
    public void UpdateDetails_ShouldUpdateFields()
    {
        // Arrange
        var product = CreateValidProduct();
        var originalUpdatedAt = product.UpdatedAt;
        var newName = "Updated Product";
        var newDescription = "Updated Description";
        var newBrand = "Updated Brand";

        // Act
        product.UpdateDetails(newName, newDescription, newBrand);

        // Assert
        product.Name.Should().Be(newName);
        product.Description.Should().Be(newDescription);
        product.Brand.Should().Be(newBrand);
        product.Slug.Should().Be("updated-product");
        product.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact(DisplayName = "Update Price: Should update price correctly")]
    public void UpdatePrice_ShouldUpdatePrice()
    {
        // Arrange
        var product = CreateValidProduct();
        var originalPrice = product.Price;
        var newPrice = 199.99m;

        // Act
        product.UpdatePrice(newPrice);

        // Assert
        product.Price.Should().Be(newPrice);
        product.Price.Should().NotBe(originalPrice);
    }

    [Fact(DisplayName = "Update Price: With compare at price should succeed")]
    public void UpdatePrice_WithCompareAtPrice_ShouldSucceed()
    {
        // Arrange
        var product = CreateValidProduct();
        var newPrice = 79.99m;
        var compareAtPrice = 99.99m;

        // Act
        product.UpdatePrice(newPrice, compareAtPrice);

        // Assert
        product.Price.Should().Be(newPrice);
        product.CompareAtPrice.Should().Be(compareAtPrice);
    }

    [Fact(DisplayName = "Update Price: With negative price should throw")]
    public void UpdatePrice_WithNegativePrice_ShouldThrow()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var act = () => product.UpdatePrice(-10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Price must be positive*");
    }

    [Fact(DisplayName = "Update Inventory: Should update available quantity")]
    public void UpdateInventory_ShouldUpdateQuantity()
    {
        // Arrange
        var product = CreateValidProduct();
        var quantity = 50;

        // Act
        product.UpdateInventory(quantity);

        // Assert
        product.AvailableQuantity.Should().Be(quantity);
    }

    #endregion

    #region Image Management Tests

    [Fact(DisplayName = "Add Image: Should add image to collection")]
    public void AddImage_ShouldAddImageToCollection()
    {
        // Arrange
        var product = CreateValidProduct();
        var url = "https://example.com/image.jpg";
        var altText = "Product Image";

        // Act
        product.AddImage(url, altText);

        // Assert
        product.Images.Should().HaveCount(1);
        product.Images.First().Url.Should().Be(url);
        product.Images.First().AltText.Should().Be(altText);
    }

    [Fact(DisplayName = "Add Image: As primary should set other images to non-primary")]
    public void AddImage_AsPrimary_ShouldSetOthersToNonPrimary()
    {
        // Arrange
        var product = CreateValidProduct();
        product.AddImage("https://example.com/image1.jpg", "Image 1", 0, true);
        product.AddImage("https://example.com/image2.jpg", "Image 2", 1, false);

        // Act
        product.AddImage("https://example.com/image3.jpg", "Image 3", 2, true);

        // Assert
        product.Images.Should().HaveCount(3);
        product.Images.Count(i => i.IsPrimary).Should().Be(1);
        product.Images.First(i => i.IsPrimary).Url.Should().Be("https://example.com/image3.jpg");
    }

    [Fact(DisplayName = "Remove Image: Should remove image from collection")]
    public void RemoveImage_ShouldRemoveImageFromCollection()
    {
        // Arrange
        var product = CreateValidProduct();
        product.AddImage("https://example.com/image.jpg", "Image");
        var imageId = product.Images.First().Id;

        // Act
        product.RemoveImage(imageId);

        // Assert
        product.Images.Should().BeEmpty();
    }

    [Fact(DisplayName = "Remove Image: With non-existent ID should not throw")]
    public void RemoveImage_WithNonExistentId_ShouldNotThrow()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var act = () => product.RemoveImage(Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Attribute Management Tests

    [Fact(DisplayName = "Add Attribute: Should add new attribute")]
    public void AddAttribute_ShouldAddNewAttribute()
    {
        // Arrange
        var product = CreateValidProduct();
        var key = "color";
        var value = "red";
        var displayName = "Color";

        // Act
        product.AddAttribute(key, value, displayName);

        // Assert
        product.Attributes.Should().HaveCount(1);
        product.Attributes.First().Key.Should().Be(key);
        product.Attributes.First().Value.Should().Be(value);
        product.Attributes.First().DisplayName.Should().Be(displayName);
    }

    [Fact(DisplayName = "Add Attribute: With existing key should update value")]
    public void AddAttribute_WithExistingKey_ShouldUpdateValue()
    {
        // Arrange
        var product = CreateValidProduct();
        var key = "color";
        product.AddAttribute(key, "red", "Color");

        // Act
        product.AddAttribute(key, "blue", "Color");

        // Assert
        product.Attributes.Should().HaveCount(1);
        product.Attributes.First().Value.Should().Be("blue");
    }

    [Fact(DisplayName = "Remove Attribute: Should remove attribute from collection")]
    public void RemoveAttribute_ShouldRemoveAttributeFromCollection()
    {
        // Arrange
        var product = CreateValidProduct();
        var key = "color";
        product.AddAttribute(key, "red", "Color");

        // Act
        product.RemoveAttribute(key);

        // Assert
        product.Attributes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Remove Attribute: With non-existent key should not throw")]
    public void RemoveAttribute_WithNonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        var act = () => product.RemoveAttribute("nonexistent");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Tag Management Tests

    [Fact(DisplayName = "Add Tag: Should add tag to collection")]
    public void AddTag_ShouldAddTagToCollection()
    {
        // Arrange
        var product = CreateValidProduct();
        var tag = "electronics";

        // Act
        product.AddTag(tag);

        // Assert
        product.Tags.Should().HaveCount(1);
        product.Tags.Should().Contain(tag);
    }

    [Fact(DisplayName = "Add Tag: Should convert to lowercase")]
    public void AddTag_ShouldConvertToLowercase()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.AddTag("ELECTRONICS");

        // Assert
        product.Tags.Should().Contain("electronics");
    }

    [Fact(DisplayName = "Add Tag: Duplicate tag should not be added")]
    public void AddTag_DuplicateTag_ShouldNotBeAdded()
    {
        // Arrange
        var product = CreateValidProduct();
        var tag = "electronics";
        product.AddTag(tag);

        // Act
        product.AddTag(tag.ToUpper());

        // Assert
        product.Tags.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Remove Tag: Should remove tag from collection")]
    public void RemoveTag_ShouldRemoveTagFromCollection()
    {
        // Arrange
        var product = CreateValidProduct();
        var tag = "electronics";
        product.AddTag(tag);

        // Act
        product.RemoveTag(tag);

        // Assert
        product.Tags.Should().BeEmpty();
    }

    #endregion

    #region Publishing Tests

    [Fact(DisplayName = "Publish: Draft product should be published")]
    public void Publish_DraftProduct_ShouldBePublished()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Status.Should().Be(ProductStatus.Draft);

        // Act
        product.Publish();

        // Assert
        product.Status.Should().Be(ProductStatus.Published);
        product.PublishedAt.Should().NotBeNull();
        product.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Publish: Already published product should not change")]
    public void Publish_AlreadyPublished_ShouldNotChange()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Publish();
        var originalPublishedAt = product.PublishedAt;

        // Act
        product.Publish();

        // Assert
        product.Status.Should().Be(ProductStatus.Published);
        product.PublishedAt.Should().Be(originalPublishedAt);
    }

    [Fact(DisplayName = "Unpublish: Published product should be unpublished")]
    public void Unpublish_PublishedProduct_ShouldBeUnpublished()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Publish();

        // Act
        product.Unpublish();

        // Assert
        product.Status.Should().Be(ProductStatus.Draft);
    }

    [Fact(DisplayName = "Unpublish: Draft product should not change")]
    public void Unpublish_DraftProduct_ShouldNotChange()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Status.Should().Be(ProductStatus.Draft);

        // Act
        product.Unpublish();

        // Assert
        product.Status.Should().Be(ProductStatus.Draft);
    }

    [Fact(DisplayName = "Archive: Should change status to archived")]
    public void Archive_ShouldChangeStatusToArchived()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.Archive();

        // Assert
        product.Status.Should().Be(ProductStatus.Archived);
    }

    #endregion

    #region Featured Tests

    [Fact(DisplayName = "Set As Featured: Should mark product as featured")]
    public void SetAsFeatured_ShouldMarkProductAsFeatured()
    {
        // Arrange
        var product = CreateValidProduct();
        product.IsFeatured.Should().BeFalse();

        // Act
        product.SetAsFeatured(true);

        // Assert
        product.IsFeatured.Should().BeTrue();
    }

    [Fact(DisplayName = "Set As Featured: Should unmark featured product")]
    public void SetAsFeatured_ShouldUnmarkFeaturedProduct()
    {
        // Arrange
        var product = CreateValidProduct();
        product.SetAsFeatured(true);

        // Act
        product.SetAsFeatured(false);

        // Assert
        product.IsFeatured.Should().BeFalse();
    }

    #endregion

    #region SEO Tests

    [Fact(DisplayName = "Update SEO: Should update SEO fields")]
    public void UpdateSeo_ShouldUpdateSeoFields()
    {
        // Arrange
        var product = CreateValidProduct();
        var seoTitle = "SEO Title";
        var seoDescription = "SEO Description";
        var slug = "custom-slug";

        // Act
        product.UpdateSeo(seoTitle, seoDescription, slug);

        // Assert
        product.SeoTitle.Should().Be(seoTitle);
        product.SeoDescription.Should().Be(seoDescription);
        product.Slug.Should().Be(slug);
    }

    [Fact(DisplayName = "Update SEO: With null values should use defaults")]
    public void UpdateSeo_WithNullValues_ShouldUseDefaults()
    {
        // Arrange
        var product = CreateValidProduct();

        // Act
        product.UpdateSeo(null, null, null);

        // Assert
        product.SeoTitle.Should().Be(product.Name);
        product.SeoDescription.Should().Be(product.Description);
        product.Slug.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private CatalogProduct CreateValidProduct()
    {
        return CatalogProduct.Create(
            "Test Product",
            "Test Description",
            "TEST-SKU-001",
            Guid.NewGuid(),
            "Test Brand",
            99.99m);
    }

    #endregion
}
