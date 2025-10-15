namespace Catalog.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    Guid CategoryId,
    string Brand,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    int AvailableQuantity,
    string Status,
    bool IsFeatured,
    string Slug,
    string SeoTitle,
    string SeoDescription,
    List<ProductImageDto> Images,
    List<ProductAttributeDto> Attributes,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt
);

public record ProductImageDto(
    Guid Id,
    string Url,
    string AltText,
    int DisplayOrder,
    bool IsPrimary
);

public record ProductAttributeDto(
    Guid Id,
    string Key,
    string Value,
    string? DisplayName
);

public record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    string Slug,
    Guid? ParentCategoryId,
    string? ImageUrl,
    bool IsActive,
    int DisplayOrder
);

public record CreateProductDto(
    string Name,
    string Description,
    string Sku,
    Guid CategoryId,
    string Brand,
    decimal Price,
    string Currency
);

public record UpdateProductDto(
    string Name,
    string Description,
    string Brand
);

public record UpdatePriceDto(
    decimal Price,
    decimal? CompareAtPrice
);

public record AddImageDto(
    string Url,
    string AltText,
    bool IsPrimary
);

public record AddAttributeDto(
    string Key,
    string Value,
    string? DisplayName
);
