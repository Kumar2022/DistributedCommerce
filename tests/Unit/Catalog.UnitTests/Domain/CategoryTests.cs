using AutoFixture;
using Bogus;
using Catalog.Domain.Aggregates;
using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests.Domain;

/// <summary>
/// Comprehensive unit tests for Category aggregate root
/// Tests category lifecycle, hierarchy, and activation
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class CategoryTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public CategoryTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Category Creation Tests

    [Fact(DisplayName = "Create Category: With valid data should succeed")]
    public void CreateCategory_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Electronics";
        var description = "Electronic devices and accessories";

        // Act
        var category = Category.Create(name, description);

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBe(Guid.Empty);
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.ParentCategoryId.Should().BeNull();
        category.Slug.Should().Be("electronics");
        category.IsActive.Should().BeTrue();
        category.DisplayOrder.Should().Be(0);
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Create Category: With parent category should succeed")]
    public void CreateCategory_WithParentCategory_ShouldSucceed()
    {
        // Arrange
        var name = "Smartphones";
        var description = "Mobile phones and smartphones";
        var parentCategoryId = Guid.NewGuid();

        // Act
        var category = Category.Create(name, description, parentCategoryId);

        // Assert
        category.ParentCategoryId.Should().Be(parentCategoryId);
    }

    [Theory(DisplayName = "Create Category: With empty name should throw")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCategory_WithEmptyName_ShouldThrow(string name)
    {
        // Arrange & Act
        var act = () => Category.Create(name, "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name is required*");
    }

    #endregion

    #region Category Update Tests

    [Fact(DisplayName = "Update: Should update name and description")]
    public void Update_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalUpdatedAt = category.UpdatedAt;
        var newName = "Updated Electronics";
        var newDescription = "Updated description";

        // Act
        category.Update(newName, newDescription);

        // Assert
        category.Name.Should().Be(newName);
        category.Description.Should().Be(newDescription);
        category.Slug.Should().Be("updated-electronics");
        category.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact(DisplayName = "Set Parent Category: Should set parent category ID")]
    public void SetParentCategory_ShouldSetParentCategoryId()
    {
        // Arrange
        var category = CreateValidCategory();
        var parentCategoryId = Guid.NewGuid();

        // Act
        category.SetParentCategory(parentCategoryId);

        // Assert
        category.ParentCategoryId.Should().Be(parentCategoryId);
    }

    [Fact(DisplayName = "Set Parent Category: To null should clear parent")]
    public void SetParentCategory_ToNull_ShouldClearParent()
    {
        // Arrange
        var category = Category.Create("Subcategory", "Description", Guid.NewGuid());
        category.ParentCategoryId.Should().NotBeNull();

        // Act
        category.SetParentCategory(null);

        // Assert
        category.ParentCategoryId.Should().BeNull();
    }

    [Fact(DisplayName = "Set Display Order: Should set display order")]
    public void SetDisplayOrder_ShouldSetDisplayOrder()
    {
        // Arrange
        var category = CreateValidCategory();
        var displayOrder = 5;

        // Act
        category.SetDisplayOrder(displayOrder);

        // Assert
        category.DisplayOrder.Should().Be(displayOrder);
    }

    [Fact(DisplayName = "Set Image: Should set image URL")]
    public void SetImage_ShouldSetImageUrl()
    {
        // Arrange
        var category = CreateValidCategory();
        var imageUrl = "https://example.com/category.jpg";

        // Act
        category.SetImage(imageUrl);

        // Assert
        category.ImageUrl.Should().Be(imageUrl);
    }

    #endregion

    #region Activation Tests

    [Fact(DisplayName = "Activate: Should set is active to true")]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var category = CreateValidCategory();
        category.Deactivate();
        category.IsActive.Should().BeFalse();

        // Act
        category.Activate();

        // Assert
        category.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "Deactivate: Should set is active to false")]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var category = CreateValidCategory();
        category.IsActive.Should().BeTrue();

        // Act
        category.Deactivate();

        // Assert
        category.IsActive.Should().BeFalse();
    }

    #endregion

    #region Slug Generation Tests

    [Theory(DisplayName = "Slug Generation: Should generate correct slug")]
    [InlineData("Electronics", "electronics")]
    [InlineData("Home & Garden", "home-and-garden")]
    [InlineData("Women's Fashion", "womens-fashion")]
    [InlineData("Books, Movies & Music", "books-movies-and-music")]
    [InlineData("Health & Beauty", "health-and-beauty")]
    public void SlugGeneration_ShouldGenerateCorrectSlug(string name, string expectedSlug)
    {
        // Arrange & Act
        var category = Category.Create(name, "Description");

        // Assert
        category.Slug.Should().Be(expectedSlug);
    }

    #endregion

    #region Helper Methods

    private Category CreateValidCategory()
    {
        return Category.Create("Electronics", "Electronic devices");
    }

    #endregion
}
