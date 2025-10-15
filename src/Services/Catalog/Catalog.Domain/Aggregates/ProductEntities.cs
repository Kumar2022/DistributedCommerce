namespace Catalog.Domain.Aggregates;

/// <summary>
/// Product Image Entity
/// </summary>
public class ProductImage : Entity<Guid>
{
    public string Url { get; private set; }
    public string AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    private ProductImage() { }

    private ProductImage(string url, string altText, int displayOrder, bool isPrimary)
    {
        Id = Guid.NewGuid();
        Url = url;
        AltText = altText;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public static ProductImage Create(string url, string altText, int displayOrder = 0, bool isPrimary = false)
    {
        return string.IsNullOrWhiteSpace(url) ? throw new ArgumentException("Image URL is required", nameof(url)) : new ProductImage(url, altText, displayOrder, isPrimary);
    }

    public void SetAsPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}

/// <summary>
/// Product Attribute Entity
/// Flexible key-value pairs for product specifications (e.g., color, size, weight)
/// </summary>
public class ProductAttribute : Entity<Guid>
{
    public string Key { get; private set; }
    public string Value { get; private set; }
    public string DisplayName { get; private set; }

    private ProductAttribute() { }

    private ProductAttribute(string key, string value, string displayName)
    {
        Id = Guid.NewGuid();
        Key = key;
        Value = value;
        DisplayName = displayName;
    }

    public static ProductAttribute Create(string key, string value, string displayName)
    {
        return string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Attribute key is required", nameof(key)) : new ProductAttribute(key, value, displayName ?? key);
    }

    public void UpdateValue(string value)
    {
        Value = value;
    }
}
