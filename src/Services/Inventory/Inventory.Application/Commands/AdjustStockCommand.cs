using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

public record AdjustStockCommand(
    Guid ProductId,
    int Quantity,
    string Reason) : ICommand;

public class AdjustStockCommandHandler : ICommandHandler<AdjustStockCommand>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<AdjustStockCommandHandler> _logger;

    public AdjustStockCommandHandler(
        IProductRepository repository,
        ILogger<AdjustStockCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adjusting stock: ProductId={ProductId}, Quantity={Quantity}, Reason={Reason}",
            request.ProductId, request.Quantity, request.Reason);

        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
            return Result.Failure(Error.NotFound("Product", request.ProductId));
        }

        var result = product.AdjustStock(request.Quantity, request.Reason);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to adjust stock for ProductId={ProductId}: {Error}",
                request.ProductId, result.Error);
            return result;
        }

        _repository.Update(product);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Stock adjusted successfully: ProductId={ProductId}, Adjustment={Quantity}",
            request.ProductId, request.Quantity);
        return Result.Success();
    }
}
