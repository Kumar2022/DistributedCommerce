namespace Shipping.Domain.Exceptions;

public class ShipmentDomainException : DomainException
{
    public ShipmentDomainException(string message) : base(message) { }
    
    public ShipmentDomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class ShipmentNotFoundException : ShipmentDomainException
{
    public ShipmentNotFoundException(Guid shipmentId) 
        : base($"Shipment with ID {shipmentId} was not found") { }
    
    public ShipmentNotFoundException(string trackingNumber) 
        : base($"Shipment with tracking number {trackingNumber} was not found") { }
}

public class InvalidShipmentStatusException : ShipmentDomainException
{
    public InvalidShipmentStatusException(string operation, string currentStatus) 
        : base($"Cannot perform {operation} when shipment is in {currentStatus} status") { }
}

public class DuplicateTrackingNumberException : ShipmentDomainException
{
    public DuplicateTrackingNumberException(string trackingNumber) 
        : base($"A shipment with tracking number {trackingNumber} already exists") { }
}
