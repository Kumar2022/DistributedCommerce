namespace Catalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CatalogProduct aggregate
/// </summary>
public class CatalogProductRepository(
    CatalogDbContext context,
    ILogger<CatalogProductRepository> logger)
    : ICatalogProductRepository
{
    public BuildingBlocks.Domain.IUnitOfWork UnitOfWork => (BuildingBlocks.Domain.IUnitOfWork)context;

    public async Task<CatalogProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<CatalogProduct?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<CatalogProduct?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<CatalogProduct>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => p.CategoryId == categoryId && p.Status == ProductStatus.Published)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CatalogProduct>> GetFeaturedProductsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => p.IsFeatured && p.Status == ProductStatus.Published)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CatalogProduct>> GetPublishedProductsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => p.Status == ProductStatus.Published)
            .OrderByDescending(p => p.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CatalogProduct>> SearchAsync(
        string? searchTerm,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var query = context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => p.Status == ProductStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lowerSearchTerm) ||
                p.Description.ToLower().Contains(lowerSearchTerm) ||
                p.Brand.ToLower().Contains(lowerSearchTerm) ||
                p.Sku.ToLower().Contains(lowerSearchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        await context.Products.AddAsync(product, cancellationToken);
        logger.LogInformation("Adding product {ProductId} to catalog", product.Id);
    }

    public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        context.Products.Update(product);
        logger.LogInformation("Updating product {ProductId} in catalog", product.Id);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product != null)
        {
            context.Products.Remove(product);
            logger.LogInformation("Deleting product {ProductId} from catalog", id);
        }
    }
}
