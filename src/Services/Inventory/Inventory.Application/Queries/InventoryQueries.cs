using Inventory.Application.DTOs;

namespace Inventory.Application.Queries;

public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;

public record GetProductBySkuQuery(string Sku) : IQuery<ProductDto>;

public record GetLowStockProductsQuery(int Threshold = 10) : IQuery<List<ProductDto>>;

public interface IInventoryQueryService
{
    Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetLowStockProductsAsync(int threshold, CancellationToken cancellationToken = default);
}

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IInventoryQueryService _queryService;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IInventoryQueryService queryService,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying product by ID: {ProductId}", request.ProductId);

        var product = await _queryService.GetProductByIdAsync(request.ProductId, cancellationToken);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
            return Result.Failure<ProductDto>(Error.NotFound("Product", request.ProductId));
        }

        return Result.Success(product);
    }
}

public class GetProductBySkuQueryHandler : IQueryHandler<GetProductBySkuQuery, ProductDto>
{
    private readonly IInventoryQueryService _queryService;
    private readonly ILogger<GetProductBySkuQueryHandler> _logger;

    public GetProductBySkuQueryHandler(
        IInventoryQueryService queryService,
        ILogger<GetProductBySkuQueryHandler> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying product by SKU: {Sku}", request.Sku);

        var product = await _queryService.GetProductBySkuAsync(request.Sku, cancellationToken);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found with SKU: {Sku}", request.Sku);
            return Result.Failure<ProductDto>(Error.NotFound("Product", request.Sku));
        }

        return Result.Success(product);
    }
}

public class GetLowStockProductsQueryHandler : IQueryHandler<GetLowStockProductsQuery, List<ProductDto>>
{
    private readonly IInventoryQueryService _queryService;
    private readonly ILogger<GetLowStockProductsQueryHandler> _logger;

    public GetLowStockProductsQueryHandler(
        IInventoryQueryService queryService,
        ILogger<GetLowStockProductsQueryHandler> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    public async Task<Result<List<ProductDto>>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying low stock products with threshold: {Threshold}", request.Threshold);

        var products = await _queryService.GetLowStockProductsAsync(request.Threshold, cancellationToken);
        
        _logger.LogInformation("Found {Count} low stock products", products.Count);
        return Result.Success(products);
    }
}
