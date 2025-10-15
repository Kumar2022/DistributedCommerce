namespace Catalog.Domain.Aggregates;

/// <summary>
/// Product Category Aggregate Root
/// Hierarchical category structure for organizing products
/// </summary>
public class Category : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string Slug { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public string ImageUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Category() { }

    private Category(Guid id, string name, string description, Guid? parentCategoryId)
    {
        Id = id;
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
        Slug = GenerateSlug(name);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Category Create(string name, string description, Guid? parentCategoryId = null)
    {
        return string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Category name is required", nameof(name)) : new Category(Guid.NewGuid(), name, description, parentCategoryId);
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        Slug = GenerateSlug(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentCategory(Guid? parentCategoryId)
    {
        ParentCategoryId = parentCategoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(",", "")
            .Replace(".", "")
            .Trim();
    }
}
