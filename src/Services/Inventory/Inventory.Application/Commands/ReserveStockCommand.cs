using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

public record ReserveStockCommand(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    int ExpirationMinutes = 15) : ICommand;

public class ReserveStockCommandHandler : ICommandHandler<ReserveStockCommand>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        IProductRepository repository,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reserving stock: ProductId={ProductId}, OrderId={OrderId}, Quantity={Quantity}",
            request.ProductId, request.OrderId, request.Quantity);

        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
            return Result.Failure(Error.NotFound("Product", request.ProductId));
        }

        var result = product.ReserveStock(
            request.OrderId,
            request.Quantity,
            TimeSpan.FromMinutes(request.ExpirationMinutes));

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to reserve stock for ProductId={ProductId}: {Error}",
                request.ProductId, result.Error);
            return result;
        }

        _repository.Update(product);
        
        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Stock reserved successfully: ProductId={ProductId}, OrderId={OrderId}",
            request.ProductId, request.OrderId);
        return Result.Success();
    }
}
