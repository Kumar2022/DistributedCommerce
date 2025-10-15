using Inventory.Infrastructure.Persistence;

namespace Inventory.Infrastructure.Services;

public class InventoryQueryService : IInventoryQueryService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryQueryService> _logger;

    public InventoryQueryService(
        InventoryDbContext context,
        ILogger<InventoryQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

        return product != null ? MapToDto(product) : null;
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(int threshold, CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity - p.ReservedQuantity <= threshold)
            .OrderBy(p => p.StockQuantity - p.ReservedQuantity)
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto).ToList();
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.ProductId,
            product.Sku,
            product.Name,
            product.StockQuantity,
            product.ReservedQuantity,
            product.AvailableQuantity,
            product.ReorderLevel,
            product.ReorderQuantity,
            product.LastRestockDate,
            product.CreatedAt);
    }
}
