using Catalog.Application.DTOs;

namespace Catalog.Application.Queries;

// Get Product by ID
public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;

public class GetProductByIdQueryHandler(
    ICatalogProductRepository repository,
    ILogger<GetProductByIdQueryHandler> logger)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(query.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<ProductDto>(Error.NotFound($"Product with ID {query.ProductId} not found"));
            }

            var dto = MapToDto(product);
            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product {ProductId}", query.ProductId);
            return Result.Failure<ProductDto>(Error.Unexpected("An error occurred while retrieving the product"));
        }
    }

    private static ProductDto MapToDto(CatalogProduct product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.CategoryId,
            product.Brand,
            product.Price,
            product.CompareAtPrice,
            product.Currency,
            product.AvailableQuantity,
            product.Status.ToString(),
            product.IsFeatured,
            product.Slug,
            product.SeoTitle,
            product.SeoDescription,
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).ToList(),
            product.Attributes.Select(a => new ProductAttributeDto(
                a.Id,
                a.Key,
                a.Value,
                a.DisplayName
            )).ToList(),
            product.Tags.ToList(),
            product.CreatedAt,
            product.UpdatedAt,
            product.PublishedAt
        );
    }
}

// Get Product by SKU
public record GetProductBySkuQuery(string Sku) : IQuery<ProductDto>;

public class GetProductBySkuQueryHandler(
    ICatalogProductRepository repository,
    ILogger<GetProductBySkuQueryHandler> logger)
    : IQueryHandler<GetProductBySkuQuery, ProductDto>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySkuQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetBySkuAsync(query.Sku, cancellationToken);
            if (product == null)
            {
                return Result.Failure<ProductDto>(Error.NotFound($"Product with SKU {query.Sku} not found"));
            }

            var dto = MapToDto(product);
            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product by SKU {Sku}", query.Sku);
            return Result.Failure<ProductDto>(Error.Unexpected("An error occurred while retrieving the product"));
        }
    }

    private static ProductDto MapToDto(CatalogProduct product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.CategoryId,
            product.Brand,
            product.Price,
            product.CompareAtPrice,
            product.Currency,
            product.AvailableQuantity,
            product.Status.ToString(),
            product.IsFeatured,
            product.Slug,
            product.SeoTitle,
            product.SeoDescription,
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).ToList(),
            product.Attributes.Select(a => new ProductAttributeDto(
                a.Id,
                a.Key,
                a.Value,
                a.DisplayName
            )).ToList(),
            product.Tags.ToList(),
            product.CreatedAt,
            product.UpdatedAt,
            product.PublishedAt
        );
    }
}

// Get Products by Category
public record GetProductsByCategoryQuery(Guid CategoryId, int PageNumber = 1, int PageSize = 20) : IQuery<List<ProductDto>>;

public class GetProductsByCategoryQueryHandler(
    ICatalogProductRepository repository,
    ILogger<GetProductsByCategoryQueryHandler> logger)
    : IQueryHandler<GetProductsByCategoryQuery, List<ProductDto>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductsByCategoryQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var products = await repository.GetByCategoryAsync(query.CategoryId, cancellationToken);
            
            var paginatedProducts = products
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtos = paginatedProducts.Select(MapToDto).ToList();
            return Result<List<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products for category {CategoryId}", query.CategoryId);
            return Result.Failure<List<ProductDto>>(Error.Unexpected("An error occurred while retrieving products"));
        }
    }

    private static ProductDto MapToDto(CatalogProduct product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.CategoryId,
            product.Brand,
            product.Price,
            product.CompareAtPrice,
            product.Currency,
            product.AvailableQuantity,
            product.Status.ToString(),
            product.IsFeatured,
            product.Slug,
            product.SeoTitle,
            product.SeoDescription,
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).ToList(),
            product.Attributes.Select(a => new ProductAttributeDto(
                a.Id,
                a.Key,
                a.Value,
                a.DisplayName
            )).ToList(),
            product.Tags.ToList(),
            product.CreatedAt,
            product.UpdatedAt,
            product.PublishedAt
        );
    }
}

// Get Featured Products
public record GetFeaturedProductsQuery(int Count = 10) : IQuery<List<ProductDto>>;

public class GetFeaturedProductsQueryHandler(
    ICatalogProductRepository repository,
    ILogger<GetFeaturedProductsQueryHandler> logger)
    : IQueryHandler<GetFeaturedProductsQuery, List<ProductDto>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetFeaturedProductsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var products = await repository.GetFeaturedProductsAsync(query.Count, cancellationToken);
            var dtos = products.Select(MapToDto).ToList();
            return Result<List<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving featured products");
            return Result.Failure<List<ProductDto>>(Error.Unexpected("An error occurred while retrieving featured products"));
        }
    }

    private static ProductDto MapToDto(CatalogProduct product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.CategoryId,
            product.Brand,
            product.Price,
            product.CompareAtPrice,
            product.Currency,
            product.AvailableQuantity,
            product.Status.ToString(),
            product.IsFeatured,
            product.Slug,
            product.SeoTitle,
            product.SeoDescription,
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).ToList(),
            product.Attributes.Select(a => new ProductAttributeDto(
                a.Id,
                a.Key,
                a.Value,
                a.DisplayName
            )).ToList(),
            product.Tags.ToList(),
            product.CreatedAt,
            product.UpdatedAt,
            product.PublishedAt
        );
    }
}

// Search Products
public record SearchProductsQuery(
    string? SearchTerm, 
    Guid? CategoryId, 
    decimal? MinPrice, 
    decimal? MaxPrice,
    int PageNumber = 1, 
    int PageSize = 20
) : IQuery<List<ProductDto>>;

public class SearchProductsQueryHandler(
    ICatalogProductRepository repository,
    ILogger<SearchProductsQueryHandler> logger)
    : IQueryHandler<SearchProductsQuery, List<ProductDto>>
{
    public async Task<Result<List<ProductDto>>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var products = await repository.SearchAsync(
                query.SearchTerm, 
                query.CategoryId, 
                query.MinPrice, 
                query.MaxPrice,
                cancellationToken);

            var paginatedProducts = products
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtos = paginatedProducts.Select(MapToDto).ToList();
            return Result<List<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching products with term {SearchTerm}", query.SearchTerm);
            return Result.Failure<List<ProductDto>>(Error.Unexpected("An error occurred while searching products"));
        }
    }

    private static ProductDto MapToDto(CatalogProduct product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.CategoryId,
            product.Brand,
            product.Price,
            product.CompareAtPrice,
            product.Currency,
            product.AvailableQuantity,
            product.Status.ToString(),
            product.IsFeatured,
            product.Slug,
            product.SeoTitle,
            product.SeoDescription,
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).ToList(),
            product.Attributes.Select(a => new ProductAttributeDto(
                a.Id,
                a.Key,
                a.Value,
                a.DisplayName
            )).ToList(),
            product.Tags.ToList(),
            product.CreatedAt,
            product.UpdatedAt,
            product.PublishedAt
        );
    }
}
