using Catalog.Domain.Events;

namespace Catalog.Domain.Aggregates;

/// <summary>
/// Catalog Product Aggregate Root
/// Represents a product in the catalog with rich metadata, pricing, and search attributes
/// </summary>
public class CatalogProduct : AggregateRoot<Guid>
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductAttribute> _attributes = [];
    private readonly List<string> _tags = [];

    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Sku { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Brand { get; private set; }
    
    // Pricing
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public string Currency { get; private set; }
    
    // Inventory reference (managed by Inventory Service)
    public int AvailableQuantity { get; private set; }
    
    // Product details
    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyList<ProductAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    
    // Status
    public ProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    
    // SEO
    public string SeoTitle { get; private set; }
    public string SeoDescription { get; private set; }
    public string Slug { get; private set; }
    
    // Metadata
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    private CatalogProduct() { } // EF Core

    private CatalogProduct(
        Guid id,
        string name,
        string description,
        string sku,
        Guid categoryId,
        string brand,
        decimal price,
        string currency)
    {
        Id = id;
        Name = name;
        Description = description;
        Sku = sku;
        CategoryId = categoryId;
        Brand = brand;
        Price = price;
        Currency = currency;
        Status = ProductStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Slug = GenerateSlug(name);
        SeoTitle = name;
        SeoDescription = description;

        AddDomainEvent(new ProductCreatedEvent(Id, Name, Sku, CategoryId));
    }

    public static CatalogProduct Create(
        string name,
        string description,
        string sku,
        Guid categoryId,
        string brand,
        decimal price,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required", nameof(name));
        
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required", nameof(sku));
        
        return price < 0 ? throw new ArgumentException("Price must be positive", nameof(price)) : new CatalogProduct(Guid.NewGuid(), name, description, sku, categoryId, brand, price, currency);
    }

    public void UpdateDetails(string name, string description, string brand)
    {
        Name = name;
        Description = description;
        Brand = brand;
        UpdatedAt = DateTime.UtcNow;
        Slug = GenerateSlug(name);
        
        AddDomainEvent(new ProductUpdatedEvent(Id, Name, Description));
    }

    public void UpdatePrice(decimal price, decimal? compareAtPrice = null)
    {
        if (price < 0)
            throw new ArgumentException("Price must be positive", nameof(price));

        var oldPrice = Price;
        Price = price;
        CompareAtPrice = compareAtPrice;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, Price));
    }

    public void UpdateInventory(int quantity)
    {
        AvailableQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddImage(string url, string altText, int displayOrder = 0, bool isPrimary = false)
    {
        if (isPrimary)
        {
            foreach (var img in _images)
            {
                img.SetAsPrimary(false);
            }
        }

        var image = ProductImage.Create(url, altText, displayOrder, isPrimary);
        _images.Add(image);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return;
        _images.Remove(image);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAttribute(string key, string value, string displayName)
    {
        var existing = _attributes.FirstOrDefault(a => a.Key == key);
        if (existing != null)
        {
            existing.UpdateValue(value);
        }
        else
        {
            _attributes.Add(ProductAttribute.Create(key, value, displayName));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAttribute(string key)
    {
        var attribute = _attributes.FirstOrDefault(a => a.Key == key);
        if (attribute == null) return;
        _attributes.Remove(attribute);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(string tag)
    {
        if (_tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) return;
        _tags.Add(tag.ToLowerInvariant());
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTag(string tag)
    {
        _tags.Remove(tag.ToLowerInvariant());
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Published) return;
        Status = ProductStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
            
        AddDomainEvent(new ProductPublishedEvent(Id, Name));
    }

    public void Unpublish()
    {
        if (Status != ProductStatus.Published) return;
        Status = ProductStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
            
        AddDomainEvent(new ProductUnpublishedEvent(Id));
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSeo(string seoTitle, string seoDescription, string slug)
    {
        SeoTitle = seoTitle ?? Name;
        SeoDescription = seoDescription ?? Description;
        Slug = !string.IsNullOrWhiteSpace(slug) ? slug : GenerateSlug(Name);
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
            .Replace("!", "")
            .Replace("?", "")
            .Trim();
    }
}

public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
