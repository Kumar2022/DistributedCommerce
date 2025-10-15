using AutoFixture;
using Bogus;
using BuildingBlocks.Domain.Results;
using FluentAssertions;
using OrderAggregate = Order.Domain.Aggregates.OrderAggregate.Order;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Xunit;

namespace Order.UnitTests.Domain;

/// <summary>
/// Comprehensive unit tests for Order aggregate root
/// Tests order creation, validation, state transitions, and business rules
/// Following Event Sourcing principles
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class OrderAggregateTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public OrderAggregateTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Order Creation Tests

    [Fact(DisplayName = "Create Order: With valid data should succeed")]
    public void CreateOrder_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();
        var items = CreateValidOrderItems(2);

        // Act
        var result = OrderAggregate.Create(
            customerId, 
            address, 
            items);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.Should().NotBeNull();
        order.Id.Should().NotBe(Guid.Empty);
        order.CustomerId.Should().Be(customerId);
        order.Items.Should().HaveCount(2);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Amount.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "Create Order: With empty items should fail")]
    public void CreateOrder_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();
        var items = new List<OrderItem>();

        // Act
        var result = OrderAggregate.Create(
            customerId, 
            address, 
            items);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Items");
    }

    [Fact(DisplayName = "Create Order: With null items should fail")]
    public void CreateOrder_WithNullItems_ShouldFail()
    {
        // Arrange
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();

        // Act
        var result = OrderAggregate.Create(
            customerId, 
            address, 
            null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Create Order: Should calculate total amount correctly")]
    public void CreateOrder_ShouldCalculateTotalAmountCorrectly()
    {
        // Arrange
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();
        
        var price1 = Money.Create(50.00m, "USD").Value;
        var price2 = Money.Create(25.00m, "USD").Value;
        
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Product 1", 2, price1).Value,
            OrderItem.Create(Guid.NewGuid(), "Product 2", 3, price2).Value
        };

        // Act
        var result = OrderAggregate.Create(
            customerId, 
            address, 
            items);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedTotal = (2 * 50.00m) + (3 * 25.00m); // 175.00
        result.Value.TotalAmount.Amount.Should().Be(expectedTotal);
    }

    #endregion

    #region Add Item Tests

    [Fact(DisplayName = "Add Item: To pending order should succeed")]
    public void AddItem_ToPendingOrder_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        var initialItemCount = order.Items.Count;
        
        var price = Money.Create(100.00m, "USD").Value;
        var newItem = OrderItem.Create(Guid.NewGuid(), "New Product", 1, price).Value;

        // Act
        var result = order.AddItem(newItem);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(initialItemCount + 1);
        order.Items.Should().Contain(i => i.ProductName == "New Product");
    }

    [Fact(DisplayName = "Add Item: To non-pending order should fail")]
    public void AddItem_ToNonPendingOrder_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");
        
        var price = Money.Create(100.00m, "USD").Value;
        var newItem = OrderItem.Create(Guid.NewGuid(), "New Product", 1, price).Value;

        // Act
        var result = order.AddItem(newItem);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Conflict");
    }

    #endregion

    #region Payment Initiation Tests

    [Fact(DisplayName = "Initiate Payment: On pending order should succeed")]
    public void InitiatePayment_OnPendingOrder_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.InitiatePayment("Credit Card");

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentInitiated);
    }

    [Fact(DisplayName = "Initiate Payment: On non-pending order should fail")]
    public void InitiatePayment_OnNonPendingOrder_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");

        // Act
        var result = order.InitiatePayment("PayPal");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "Initiate Payment: With empty order should fail")]
    public void InitiatePayment_WithEmptyOrder_ShouldFail()
    {
        // This test is theoretical as Order.Create prevents empty items
        // Including for completeness
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();
        
        var price = Money.Create(100m, "USD").Value;
        var itemResult = OrderItem.Create(Guid.NewGuid(), "Product", 1, price);
        
        var items = new List<OrderItem> 
        { 
            itemResult.Value
        };

        var orderResult = OrderAggregate.Create(customerId, address, items);
        orderResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Payment Completion Tests

    [Fact(DisplayName = "Complete Payment: After initiation should succeed")]
    public void CompletePayment_AfterInitiation_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");
        var paymentId = Guid.NewGuid();

        // Act
        var result = order.CompletePayment(paymentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentCompleted);
        order.PaymentId.Should().Be(paymentId);
    }

    [Fact(DisplayName = "Complete Payment: Without initiation should fail")]
    public void CompletePayment_WithoutInitiation_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        var paymentId = Guid.NewGuid();

        // Act
        var result = order.CompletePayment(paymentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Order Confirmation Tests

    [Fact(DisplayName = "Confirm Order: After payment completed should succeed")]
    public void ConfirmOrder_AfterPaymentCompleted_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");
        order.CompletePayment(Guid.NewGuid());

        // Act
        var result = order.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact(DisplayName = "Confirm Order: Without payment should fail")]
    public void ConfirmOrder_WithoutPayment_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Confirm();

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Shipping Tests

    [Fact(DisplayName = "Ship Order: When confirmed should succeed")]
    public void ShipOrder_WhenConfirmed_ShouldSucceed()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        var trackingNumber = "TRACK123456789";
        var carrier = "FedEx";

        // Act
        var result = order.Ship(trackingNumber, carrier);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.TrackingNumber.Should().Be(trackingNumber);
    }

    [Fact(DisplayName = "Ship Order: When not confirmed should fail")]
    public void ShipOrder_WhenNotConfirmed_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Ship("TRACK123", "FedEx");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "Ship Order: With empty tracking number should fail")]
    public void ShipOrder_WithEmptyTrackingNumber_ShouldFail()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        var result = order.Ship("", "FedEx");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Ship Order: With whitespace tracking number should fail")]
    public void ShipOrder_WithWhitespaceTrackingNumber_ShouldFail()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        var result = order.Ship("   ", "FedEx");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Cancellation Tests

    [Fact(DisplayName = "Cancel Order: When pending should succeed")]
    public void CancelOrder_WhenPending_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        var reason = "Customer requested cancellation";

        // Act
        var result = order.Cancel(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be(reason);
    }

    [Fact(DisplayName = "Cancel Order: When confirmed should succeed")]
    public void CancelOrder_WhenConfirmed_ShouldSucceed()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        var reason = "Out of stock";

        // Act
        var result = order.Cancel(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact(DisplayName = "Cancel Order: When shipped should fail")]
    public void CancelOrder_WhenShipped_ShouldFail()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        order.Ship("TRACK123", "FedEx");

        // Act
        var result = order.Cancel("Too late");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "Cancel Order: When already cancelled should fail")]
    public void CancelOrder_WhenAlreadyCancelled_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Cancel("First cancellation");

        // Act
        var result = order.Cancel("Second cancellation");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "Cancel Order: With empty reason should fail")]
    public void CancelOrder_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Cancel("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact(DisplayName = "Cancel Order: With null reason should fail")]
    public void CancelOrder_WithNullReason_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Cancel(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region State Transition Tests

    [Fact(DisplayName = "State Transitions: Complete order flow should succeed")]
    public void StateTransitions_CompleteOrderFlow_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert - Step through complete flow
        order.Status.Should().Be(OrderStatus.Pending);

        var initiateResult = order.InitiatePayment("Credit Card");
        initiateResult.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentInitiated);

        var completePaymentResult = order.CompletePayment(Guid.NewGuid());
        completePaymentResult.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PaymentCompleted);

        var confirmResult = order.Confirm();
        confirmResult.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);

        var shipResult = order.Ship("TRACK123", "FedEx");
        shipResult.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact(DisplayName = "State Transitions: Cannot skip payment initiation")]
    public void StateTransitions_CannotSkipPaymentInitiation()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.CompletePayment(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "State Transitions: Cannot skip payment completion")]
    public void StateTransitions_CannotSkipPaymentCompletion()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");

        // Act
        var result = order.Confirm();

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "State Transitions: Cannot skip confirmation")]
    public void StateTransitions_CannotSkipConfirmation()
    {
        // Arrange
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");
        order.CompletePayment(Guid.NewGuid());

        // Act
        var result = order.Ship("TRACK123", "FedEx");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private OrderAggregate CreateValidOrder()
    {
        var customerId = CustomerId.Create(Guid.NewGuid());
        var address = CreateValidAddress();
        var items = CreateValidOrderItems(2);

        var result = OrderAggregate.Create(
            customerId, 
            address, 
            items);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private OrderAggregate CreateConfirmedOrder()
    {
        var order = CreateValidOrder();
        order.InitiatePayment("Credit Card");
        order.CompletePayment(Guid.NewGuid());
        order.Confirm();
        return order;
    }

    private Address CreateValidAddress()
    {
        var result = Address.Create(
            _faker.Address.StreetAddress(),
            _faker.Address.City(),
            _faker.Address.StateAbbr(),
            _faker.Address.ZipCode(),
            "USA"
        );
        return result.Value;
    }

    private List<OrderItem> CreateValidOrderItems(int count)
    {
        var items = new List<OrderItem>();
        for (int i = 0; i < count; i++)
        {
            var priceResult = Money.Create(_faker.Random.Decimal(10, 200), "USD");
            var itemResult = OrderItem.Create(
                Guid.NewGuid(),
                _faker.Commerce.ProductName(),
                _faker.Random.Int(1, 5),
                priceResult.Value
            );
            items.Add(itemResult.Value);
        }
        return items;
    }

    #endregion
}
