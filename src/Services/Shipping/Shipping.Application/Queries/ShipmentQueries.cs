namespace Shipping.Application.Queries;

public record GetShipmentByIdQuery(Guid ShipmentId) : IQuery<ShipmentDto>;

public class GetShipmentByIdQueryHandler : IQueryHandler<GetShipmentByIdQuery, ShipmentDto>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<GetShipmentByIdQueryHandler> _logger;

    public GetShipmentByIdQueryHandler(
        IShipmentRepository repository,
        ILogger<GetShipmentByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ShipmentDto>> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return Result.Failure<ShipmentDto>(Error.NotFound("Shipment", "Shipment not found"));

            var dto = MapToDto(shipment);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment {ShipmentId}", request.ShipmentId);
            return Result.Failure<ShipmentDto>(Error.Unexpected("Failed to retrieve shipment"));
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

public record GetShipmentByTrackingNumberQuery(string TrackingNumber) : IQuery<ShipmentDto>;

public class GetShipmentByTrackingNumberQueryHandler : IQueryHandler<GetShipmentByTrackingNumberQuery, ShipmentDto>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<GetShipmentByTrackingNumberQueryHandler> _logger;

    public GetShipmentByTrackingNumberQueryHandler(
        IShipmentRepository repository,
        ILogger<GetShipmentByTrackingNumberQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ShipmentDto>> Handle(GetShipmentByTrackingNumberQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByTrackingNumberAsync(request.TrackingNumber, cancellationToken);
            if (shipment == null)
                return Result.Failure<ShipmentDto>(Error.NotFound("Shipment", "Shipment not found"));

            var dto = MapToDto(shipment);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment with tracking number {TrackingNumber}", request.TrackingNumber);
            return Result.Failure<ShipmentDto>(Error.Unexpected("Failed to retrieve shipment"));
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

public record GetShipmentsByOrderIdQuery(Guid OrderId) : IQuery<List<ShipmentDto>>;

public class GetShipmentsByOrderIdQueryHandler : IQueryHandler<GetShipmentsByOrderIdQuery, List<ShipmentDto>>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<GetShipmentsByOrderIdQueryHandler> _logger;

    public GetShipmentsByOrderIdQueryHandler(
        IShipmentRepository repository,
        ILogger<GetShipmentsByOrderIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<ShipmentDto>>> Handle(GetShipmentsByOrderIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _repository.GetByOrderIdAsync(request.OrderId, cancellationToken);
            if (shipment == null)
                return Result.Success(new List<ShipmentDto>());
            
            var dto = MapToDto(shipment);
            return Result.Success(new List<ShipmentDto> { dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipments for order {OrderId}", request.OrderId);
            return Result.Failure<List<ShipmentDto>>(Error.Unexpected("Failed to retrieve shipments"));
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

public record GetPendingShipmentsQuery(int PageNumber = 1, int PageSize = 20) : IQuery<List<ShipmentDto>>;

public class GetPendingShipmentsQueryHandler : IQueryHandler<GetPendingShipmentsQuery, List<ShipmentDto>>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<GetPendingShipmentsQueryHandler> _logger;

    public GetPendingShipmentsQueryHandler(
        IShipmentRepository repository,
        ILogger<GetPendingShipmentsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<ShipmentDto>>> Handle(GetPendingShipmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shipments = await _repository.GetPendingShipmentsAsync(request.PageNumber, request.PageSize, cancellationToken);
            var dtos = shipments.Select(MapToDto).ToList();
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending shipments");
            return Result.Failure<List<ShipmentDto>>(Error.Unexpected("Failed to retrieve pending shipments"));
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

public record GetDelayedShipmentsQuery(int PageNumber = 1, int PageSize = 20) : IQuery<List<ShipmentDto>>;

public class GetDelayedShipmentsQueryHandler : IQueryHandler<GetDelayedShipmentsQuery, List<ShipmentDto>>
{
    private readonly IShipmentRepository _repository;
    private readonly ILogger<GetDelayedShipmentsQueryHandler> _logger;

    public GetDelayedShipmentsQueryHandler(
        IShipmentRepository repository,
        ILogger<GetDelayedShipmentsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<ShipmentDto>>> Handle(GetDelayedShipmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shipments = await _repository.GetDelayedShipmentsAsync(request.PageNumber, request.PageSize, cancellationToken);
            var dtos = shipments.Select(MapToDto).ToList();
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delayed shipments");
            return Result.Failure<List<ShipmentDto>>(Error.Unexpected("Failed to retrieve delayed shipments"));
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
