namespace Shipping.Application.Commands;

public record UpdateShipmentStatusCommand(
    Guid ShipmentId,
    string Status,
    string? Location = null,
    string? Description = null,
    string? Coordinates = null
) : ICommand;

public class UpdateShipmentStatusCommandHandler : ICommandHandler<UpdateShipmentStatusCommand>
{
    private readonly IShipmentRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UpdateShipmentStatusCommandHandler> _logger;

    public UpdateShipmentStatusCommandHandler(
        IShipmentRepository repository,
        IEventBus eventBus,
        ILogger<UpdateShipmentStatusCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure(Error.NotFound("Shipment", request.ShipmentId));

            if (!Enum.TryParse<ShipmentStatus>(request.Status, true, out var status))
                return Result.Failure(Error.Validation(nameof(request.Status), "Invalid shipment status"));

            // Update status based on the new status
            try
            {
                switch (status)
                {
                    case ShipmentStatus.PickedUp:
                        shipment.MarkAsPickedUp(DateTime.UtcNow);
                        break;
                    case ShipmentStatus.InTransit:
                        shipment.UpdateToInTransit(request.Location ?? "In Transit", null);
                        break;
                    case ShipmentStatus.OutForDelivery:
                        shipment.MarkAsOutForDelivery(null);
                        break;
                    case ShipmentStatus.Delivered:
                        shipment.MarkAsDelivered(DateTime.UtcNow, request.Description ?? "Package delivered", null);
                        break;
                    default:
                        return Result.Failure(Error.Validation(nameof(request.Status), "Invalid status transition"));
                }
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation(nameof(request.Status), ex.Message));
            }

            // Add tracking update if location provided
            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                shipment.AddTrackingUpdate(
                    request.Location,
                    request.Status,
                    request.Description ?? $"Shipment is {request.Status.ToLower()}",
                    DateTime.UtcNow,
                    request.Coordinates
                );
            }

            await _repository.UpdateAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration events
            foreach (var domainEvent in shipment.DomainEvents)
            {
                switch (domainEvent)
                {
                    case ShipmentStatusChangedEvent statusChanged:
                        await _eventBus.PublishAsync(new ShipmentStatusChangedIntegrationEvent(
                            statusChanged.ShipmentId,
                            shipment.OrderId,
                            statusChanged.OldStatus,
                            statusChanged.NewStatus,
                            statusChanged.Timestamp
                        ), cancellationToken);
                        break;

                    case ShipmentDeliveredEvent delivered:
                        await _eventBus.PublishAsync(new ShipmentDeliveredIntegrationEvent(
                            delivered.ShipmentId,
                            delivered.OrderId,
                            delivered.DeliveryTime,
                            delivered.RecipientName
                        ), cancellationToken);
                        break;
                }
            }

            _logger.LogInformation("Shipment status updated: {ShipmentId} to {Status}", 
                shipment.Id, status);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipment status for {ShipmentId}", request.ShipmentId);
            return Result.Failure(Error.Unexpected("Failed to update shipment status"));
        }
    }
}

public class UpdateShipmentStatusCommandValidator : AbstractValidator<UpdateShipmentStatusCommand>
{
    public UpdateShipmentStatusCommandValidator()
    {
        RuleFor(x => x.ShipmentId)
            .NotEmpty().WithMessage("ShipmentId is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => Enum.TryParse<ShipmentStatus>(s, true, out _))
            .WithMessage("Invalid shipment status");

        When(x => !string.IsNullOrWhiteSpace(x.Location), () =>
        {
            RuleFor(x => x.Location)
                .MaximumLength(200).WithMessage("Location must not exceed 200 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        });
    }
}

// Integration Events
public record ShipmentStatusChangedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt
) : IntegrationEvent;

public record ShipmentDeliveredIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    DateTime DeliveredAt,
    string? RecipientName
) : IntegrationEvent;
