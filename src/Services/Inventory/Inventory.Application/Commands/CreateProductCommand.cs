using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

public record CreateProductCommand(
    string Sku,
    string Name,
    int InitialStock,
    int ReorderLevel = 10,
    int ReorderQuantity = 100) : ICommand<Guid>;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository repository,
        ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product with SKU: {Sku}", request.Sku);

        var productResult = Product.Create(
            request.Sku,
            request.Name,
            request.InitialStock,
            request.ReorderLevel,
            request.ReorderQuantity);

        if (productResult.IsFailure)
        {
            _logger.LogWarning("Failed to create product: {Error}", productResult.Error);
            return Result.Failure<Guid>(productResult.Error);
        }

        await _repository.AddAsync(productResult.Value!, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created successfully: {ProductId}", productResult.Value!.ProductId);
        return Result.Success(productResult.Value!.ProductId);
    }
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
