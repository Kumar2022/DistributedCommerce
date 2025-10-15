namespace Shipping.Application.Commands;

public record CreateShipmentCommand(
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    ShippingAddressDto ShippingAddress,
    PackageDto Package,
    string DeliverySpeed,
    decimal ShippingCost,
    string Currency = "USD"
) : ICommand<ShipmentDto>;

public class CreateShipmentCommandHandler : ICommandHandler<CreateShipmentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateShipmentCommandHandler> _logger;

    public CreateShipmentCommandHandler(
        IShipmentRepository repository,
        IEventBus eventBus,
        ILogger<CreateShipmentCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<ShipmentDto>> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create value objects
            var shippingAddress = new ShippingAddress(
                request.ShippingAddress.RecipientName,
                request.ShippingAddress.Phone,
                request.ShippingAddress.AddressLine1,
                request.ShippingAddress.City,
                request.ShippingAddress.StateOrProvince,
                request.ShippingAddress.PostalCode,
                request.ShippingAddress.Country,
                request.ShippingAddress.AddressLine2,
                request.ShippingAddress.Email
            );

            var package = new Package(
                request.Package.Weight,
                request.Package.Length,
                request.Package.Width,
                request.Package.Height
            );

            // Parse delivery speed
            if (!Enum.TryParse<DeliverySpeed>(request.DeliverySpeed, true, out var deliverySpeed))
                return Result.Failure<ShipmentDto>(Error.Validation(nameof(request.DeliverySpeed), "Invalid delivery speed"));

            // Parse carrier
            if (!Enum.TryParse<Carrier>(request.Carrier, true, out var carrier))
                return Result.Failure<ShipmentDto>(Error.Validation(nameof(request.Carrier), "Invalid carrier"));

            // Create shipment aggregate
            var shipment = Shipment.Create(
                request.OrderId,
                request.TrackingNumber,
                carrier,
                shippingAddress,
                package,
                deliverySpeed,
                request.ShippingCost,
                request.Currency
            );

            // Save shipment
            await _repository.AddAsync(shipment, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain events as integration events
            foreach (var domainEvent in shipment.DomainEvents)
            {
                if (domainEvent is ShipmentCreatedEvent createdEvent)
                {
                    await _eventBus.PublishAsync(new ShipmentCreatedIntegrationEvent(
                        createdEvent.ShipmentId,
                        createdEvent.OrderId,
                        createdEvent.TrackingNumber,
                        createdEvent.Carrier,
                        shipment.EstimatedDelivery ?? DateTime.UtcNow.AddDays(3)
                    ), cancellationToken);
                }
            }

            _logger.LogInformation("Shipment created successfully: {ShipmentId} for Order {OrderId}", 
                shipment.Id, request.OrderId);

            // Map to DTO
            var dto = MapToDto(shipment);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment for order {OrderId}", request.OrderId);
            return Result.Failure<ShipmentDto>(Error.Unexpected("Failed to create shipment"));
        }
    }

    private static ShipmentDto MapToDto(Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.TrackingNumber,
        shipment.Carrier.ToString(),
        shipment.Status.ToString(),
        new ShippingAddressDto(
            shipment.ShippingAddress.RecipientName,
            shipment.ShippingAddress.Phone,
            shipment.ShippingAddress.AddressLine1,
            shipment.ShippingAddress.AddressLine2,
            shipment.ShippingAddress.City,
            shipment.ShippingAddress.StateOrProvince,
            shipment.ShippingAddress.PostalCode,
            shipment.ShippingAddress.Country,
            shipment.ShippingAddress.Email
        ),
        new PackageDto(
            shipment.Package.Weight,
            shipment.Package.Length,
            shipment.Package.Width,
            shipment.Package.Height,
            shipment.Package.WeightUnit,
            shipment.Package.DimensionUnit
        ),
        shipment.DeliverySpeed.ToString(),
        shipment.ShippingCost,
        shipment.Currency,
        shipment.CreatedAt,
        shipment.PickupTime,
        shipment.EstimatedDelivery,
        shipment.ActualDelivery,
        shipment.RecipientName,
        shipment.DeliveryAttempts,
        shipment.TrackingHistory.Select(t => new TrackingInfoDto(
            t.Location,
            t.Status,
            t.Description,
            t.Timestamp,
            t.Coordinates
        )).ToList(),
        shipment.IsDelayed(),
        shipment.GetDelayHours()
    );
}

public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required");

        RuleFor(x => x.TrackingNumber)
            .NotEmpty().WithMessage("Tracking number is required")
            .MaximumLength(50).WithMessage("Tracking number must not exceed 50 characters");

        RuleFor(x => x.Carrier)
            .NotEmpty().WithMessage("Carrier is required")
            .MaximumLength(100).WithMessage("Carrier name must not exceed 100 characters");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        When(x => x.ShippingAddress != null, () =>
        {
            RuleFor(x => x.ShippingAddress.RecipientName)
                .NotEmpty().WithMessage("Recipient name is required");

            RuleFor(x => x.ShippingAddress.Phone)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

            RuleFor(x => x.ShippingAddress.AddressLine1)
                .NotEmpty().WithMessage("Address line 1 is required");

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty().WithMessage("City is required");

            RuleFor(x => x.ShippingAddress.PostalCode)
                .NotEmpty().WithMessage("Postal code is required");

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty().WithMessage("Country is required");
        });

        RuleFor(x => x.Package)
            .NotNull().WithMessage("Package information is required");

        When(x => x.Package != null, () =>
        {
            RuleFor(x => x.Package.Weight)
                .GreaterThan(0).WithMessage("Weight must be greater than 0");

            RuleFor(x => x.Package.Length)
                .GreaterThan(0).WithMessage("Length must be greater than 0");

            RuleFor(x => x.Package.Width)
                .GreaterThan(0).WithMessage("Width must be greater than 0");

            RuleFor(x => x.Package.Height)
                .GreaterThan(0).WithMessage("Height must be greater than 0");
        });

        RuleFor(x => x.DeliverySpeed)
            .NotEmpty().WithMessage("Delivery speed is required")
            .Must(s => Enum.TryParse<DeliverySpeed>(s, true, out _))
            .WithMessage("Invalid delivery speed");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping cost cannot be negative");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters (ISO 4217)");
    }
}

// Integration Event
public record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    string TrackingNumber,
    string Carrier,
    DateTime EstimatedDelivery
) : IntegrationEvent;
