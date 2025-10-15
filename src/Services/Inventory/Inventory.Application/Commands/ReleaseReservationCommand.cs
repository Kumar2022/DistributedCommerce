using Inventory.Domain.Aggregates;

namespace Inventory.Application.Commands;

public record ReleaseReservationCommand(
    Guid ProductId,
    Guid OrderId) : ICommand;

public class ReleaseReservationCommandHandler : ICommandHandler<ReleaseReservationCommand>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ReleaseReservationCommandHandler> _logger;

    public ReleaseReservationCommandHandler(
        IProductRepository repository,
        ILogger<ReleaseReservationCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Releasing stock reservation: ProductId={ProductId}, OrderId={OrderId}",
            request.ProductId, request.OrderId);

        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
            return Result.Failure(Error.NotFound("Product", request.ProductId));
        }

        var result = product.ReleaseReservation(request.OrderId);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to release reservation for ProductId={ProductId}, OrderId={OrderId}: {Error}",
                request.ProductId, request.OrderId, result.Error);
            return result;
        }

        _repository.Update(product);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Reservation released successfully: ProductId={ProductId}, OrderId={OrderId}",
            request.ProductId, request.OrderId);
        return Result.Success();
    }
}
