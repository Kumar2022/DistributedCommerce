using BuildingBlocks.Domain;
using Catalog.Domain.Aggregates;

namespace Catalog.Domain.Repositories;

/// <summary>
/// Repository interface for CatalogProduct aggregate
/// </summary>
public interface ICatalogProductRepository
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<CatalogProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CatalogProduct?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<CatalogProduct?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<CatalogProduct>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CatalogProduct>> GetFeaturedProductsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<CatalogProduct>> GetPublishedProductsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<CatalogProduct>> SearchAsync(
        string? searchTerm, 
        Guid? categoryId, 
        decimal? minPrice, 
        decimal? maxPrice,
        CancellationToken cancellationToken = default);
    Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default);
    Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Category aggregate
/// </summary>
public interface ICategoryRepository
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
