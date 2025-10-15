using BuildingBlocks.Saga.Abstractions;

namespace Order.Application.Sagas;

/// <summary>
/// State for Order Creation Saga
/// Orchestrates: Inventory Reservation → Payment Processing → Order Confirmation
/// </summary>
public class OrderCreationSagaState : SagaState
{
    // Order Information
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public List<OrderItemSagaData> OrderItems { get; set; } = new();
    public AddressSagaData ShippingAddress { get; set; } = new();
    
    // Saga Results
    public Guid? ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
    public bool IsInventoryReserved { get; set; }
    public bool IsPaymentProcessed { get; set; }
    public bool IsOrderConfirmed { get; set; }
    
    // Compensation Tracking
    public bool IsInventoryReleased { get; set; }
    public bool IsPaymentRefunded { get; set; }
    public bool IsOrderCancelled { get; set; }
}

public class OrderItemSagaData
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class AddressSagaData
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
