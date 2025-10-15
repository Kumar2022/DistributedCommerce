using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

public record ConfirmReservationCommand(
    Guid ProductId,
    Guid OrderId) : ICommand;

public class ConfirmReservationCommandHandler : ICommandHandler<ConfirmReservationCommand>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ConfirmReservationCommandHandler> _logger;

    public ConfirmReservationCommandHandler(
        IProductRepository repository,
        ILogger<ConfirmReservationCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming stock reservation: ProductId={ProductId}, OrderId={OrderId}",
            request.ProductId, request.OrderId);

        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
            return Result.Failure(Error.NotFound("Product", request.ProductId));
        }

        var result = product.ConfirmReservation(request.OrderId);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to confirm reservation for ProductId={ProductId}, OrderId={OrderId}: {Error}",
                request.ProductId, request.OrderId, result.Error);
            return result;
        }

        _repository.Update(product);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Reservation confirmed successfully: ProductId={ProductId}, OrderId={OrderId}",
            request.ProductId, request.OrderId);
        return Result.Success();
    }
}
