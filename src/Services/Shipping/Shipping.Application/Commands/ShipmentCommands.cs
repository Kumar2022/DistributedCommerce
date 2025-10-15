namespace Shipping.Application.Commands;

public record CancelShipmentCommand(Guid ShipmentId, string Reason) : ICommand;

public class CancelShipmentCommandHandler : ICommandHandler<CancelShipmentCommand>
{
    private readonly IShipmentRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CancelShipmentCommandHandler> _logger;

    public CancelShipmentCommandHandler(
        IShipmentRepository repository,
        IEventBus eventBus,
        ILogger<CancelShipmentCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure(Error.NotFound("Shipment", "Shipment not found"));

            try
            {
                shipment.Cancel(request.Reason);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation(nameof(request.ShipmentId), ex.Message));
            }

            await _repository.UpdateAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration event
            foreach (var domainEvent in shipment.DomainEvents)
            {
                if (domainEvent is ShipmentCancelledEvent cancelled)
                {
                    await _eventBus.PublishAsync(new ShipmentCancelledIntegrationEvent(
                        cancelled.ShipmentId,
                        cancelled.OrderId,
                        request.Reason,
                        cancelled.CancelledAt
                    ), cancellationToken);
                }
            }

            _logger.LogInformation("Shipment cancelled: {ShipmentId}, Reason: {Reason}", 
                shipment.Id, request.Reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling shipment {ShipmentId}", request.ShipmentId);
            return Result.Failure(Error.Unexpected("Failed to cancel shipment"));
        }
    }
}

public record MarkAsDeliveredCommand(
    Guid ShipmentId,
    string RecipientName,
    string? SignatureUrl = null
) : ICommand;

public class MarkAsDeliveredCommandHandler : ICommandHandler<MarkAsDeliveredCommand>
{
    private readonly IShipmentRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MarkAsDeliveredCommandHandler> _logger;

    public MarkAsDeliveredCommandHandler(
        IShipmentRepository repository,
        IEventBus eventBus,
        ILogger<MarkAsDeliveredCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkAsDeliveredCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure(Error.NotFound("Shipment", "Shipment not found"));

            try
            {
                shipment.MarkAsDelivered(DateTime.UtcNow, request.RecipientName, request.SignatureUrl);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation(nameof(request.ShipmentId), ex.Message));
            }

            await _repository.UpdateAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration event
            foreach (var domainEvent in shipment.DomainEvents)
            {
                if (domainEvent is ShipmentDeliveredEvent delivered)
                {
                    await _eventBus.PublishAsync(new ShipmentDeliveredIntegrationEvent(
                        delivered.ShipmentId,
                        delivered.OrderId,
                        delivered.DeliveryTime,
                        delivered.RecipientName
                    ), cancellationToken);
                }
            }

            _logger.LogInformation("Shipment marked as delivered: {ShipmentId}", shipment.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking shipment as delivered {ShipmentId}", request.ShipmentId);
            return Result.Failure(Error.Unexpected("Failed to mark shipment as delivered"));
        }
    }
}

public record AddTrackingUpdateCommand(
    Guid ShipmentId,
    string Location,
    string Status,
    string Description,
    string? Coordinates = null
) : ICommand;

public class AddTrackingUpdateCommandHandler : ICommandHandler<AddTrackingUpdateCommand>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<AddTrackingUpdateCommandHandler> _logger;

    public AddTrackingUpdateCommandHandler(
        IShipmentRepository repository,
        ILogger<AddTrackingUpdateCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(AddTrackingUpdateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure(Error.NotFound("Shipment", "Shipment not found"));

            try
            {
                shipment.AddTrackingUpdate(
                    request.Location,
                    request.Status,
                    request.Description,
                    DateTime.UtcNow,
                    request.Coordinates
                );
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(Error.Validation("TrackingUpdate", ex.Message));
            }

            await _repository.UpdateAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tracking update added to shipment: {ShipmentId}", shipment.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tracking update to shipment {ShipmentId}", request.ShipmentId);
            return Result.Failure(Error.Unexpected("Failed to add tracking update"));
        }
    }
}

public record RecordDeliveryAttemptCommand(
    Guid ShipmentId,
    string Reason
) : ICommand;

public class RecordDeliveryAttemptCommandHandler : ICommandHandler<RecordDeliveryAttemptCommand>
{
    private readonly IShipmentRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RecordDeliveryAttemptCommandHandler> _logger;

    public RecordDeliveryAttemptCommandHandler(
        IShipmentRepository repository,
        IEventBus eventBus,
        ILogger<RecordDeliveryAttemptCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(RecordDeliveryAttemptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure(Error.NotFound("Shipment", "Shipment not found"));

            try
            {
                shipment.MarkDeliveryFailed(request.Reason, null);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation(nameof(request.Reason), ex.Message));
            }

            await _repository.UpdateAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration event if delivery failed after max attempts
            foreach (var domainEvent in shipment.DomainEvents)
            {
                if (domainEvent is ShipmentDeliveryFailedEvent failed)
                {
                    await _eventBus.PublishAsync(new ShipmentDeliveryFailedIntegrationEvent(
                        failed.ShipmentId,
                        shipment.OrderId,
                        failed.Reason,
                        DateTime.UtcNow,
                        failed.AttemptNumber
                    ), cancellationToken);
                }
            }

            _logger.LogInformation("Delivery attempt recorded for shipment: {ShipmentId}, Attempts: {Attempts}", 
                shipment.Id, shipment.DeliveryAttempts);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording delivery attempt for shipment {ShipmentId}", request.ShipmentId);
            return Result.Failure(Error.Unexpected("Failed to record delivery attempt"));
        }
    }
}

// Integration Events
public record ShipmentCancelledIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime CancelledAt
) : IntegrationEvent;

public record ShipmentDeliveryFailedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime FailedAt,
    int Attempts
) : IntegrationEvent;
