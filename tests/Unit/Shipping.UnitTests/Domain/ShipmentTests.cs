using Shipping.Domain.Aggregates;
using Shipping.Domain.ValueObjects;

namespace Shipping.UnitTests.Domain;

/// <summary>
/// Unit tests for Shipment Aggregate Root
/// Tests all business rules and domain logic
/// </summary>
public class ShipmentTests
{
    private static ShippingAddress CreateTestAddress()
    {
        return new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA",
            addressLine2: "Apt 4B",
            email: "john.doe@example.com"
        );
    }

    private static Package CreateTestPackage()
    {
        return new Package(
            weight: 2.5m,
            length: 30,
            width: 20,
            height: 15
        );
    }

    #region Creation Tests

    [Fact(DisplayName = "Create: Valid shipment should succeed")]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var trackingNumber = "TRACK123456";
        var carrier = Carrier.FedEx;
        var address = CreateTestAddress();
        var package = CreateTestPackage();
        var deliverySpeed = DeliverySpeed.Express;
        var shippingCost = 15.99m;

        // Act
        var shipment = Shipment.Create(
            orderId, trackingNumber, carrier, address, package, deliverySpeed, shippingCost);

        // Assert
        shipment.Should().NotBeNull();
        shipment.Id.Should().NotBeEmpty();
        shipment.OrderId.Should().Be(orderId);
        shipment.TrackingNumber.Should().Be(trackingNumber);
        shipment.Carrier.Should().Be(carrier);
        shipment.ShippingAddress.Should().Be(address);
        shipment.Package.Should().Be(package);
        shipment.DeliverySpeed.Should().Be(deliverySpeed);
        shipment.ShippingCost.Should().Be(shippingCost);
        shipment.Currency.Should().Be("USD");
        shipment.Status.Should().Be(ShipmentStatus.Pending);
        shipment.DeliveryAttempts.Should().Be(0);
        shipment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Create: Empty order ID should throw exception")]
    public void Create_WithEmptyOrderId_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.Empty;
        var trackingNumber = "TRACK123456";
        var carrier = Carrier.FedEx;
        var address = CreateTestAddress();
        var package = CreateTestPackage();

        // Act
        var act = () => Shipment.Create(
            orderId, trackingNumber, carrier, address, package, DeliverySpeed.Standard, 10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Order ID is required*");
    }

    [Theory(DisplayName = "Create: Null or empty tracking number should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyTrackingNumber_ShouldThrowException(string? trackingNumber)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var carrier = Carrier.FedEx;
        var address = CreateTestAddress();
        var package = CreateTestPackage();

        // Act
        var act = () => Shipment.Create(
            orderId, trackingNumber!, carrier, address, package, DeliverySpeed.Standard, 10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tracking number is required*");
    }

    [Fact(DisplayName = "Create: Null address should throw exception")]
    public void Create_WithNullAddress_ShouldThrowException()
    {
        // Act
        var act = () => Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, null!, CreateTestPackage(), 
            DeliverySpeed.Standard, 10m);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create: Null package should throw exception")]
    public void Create_WithNullPackage_ShouldThrowException()
    {
        // Act
        var act = () => Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), null!, 
            DeliverySpeed.Standard, 10m);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create: Negative shipping cost should throw exception")]
    public void Create_WithNegativeShippingCost_ShouldThrowException()
    {
        // Act
        var act = () => Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, -5m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Shipping cost must be non-negative*");
    }

    [Fact(DisplayName = "Create: Custom currency should be set")]
    public void Create_WithCustomCurrency_ShouldSetCurrency()
    {
        // Act
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m, "EUR");

        // Assert
        shipment.Currency.Should().Be("EUR");
    }

    [Fact(DisplayName = "Create: Should raise ShipmentCreatedEvent")]
    public void Create_ShouldRaiseShipmentCreatedEvent()
    {
        // Act
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Assert
        shipment.DomainEvents.Should().HaveCount(1);
        var domainEvent = shipment.DomainEvents.First();
        domainEvent.Should().BeOfType<Shipping.Domain.Events.ShipmentCreatedEvent>();
    }

    #endregion

    #region Pickup Tests

    [Fact(DisplayName = "SchedulePickup: Valid future time should succeed")]
    public void SchedulePickup_WithFutureTime_ShouldSucceed()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        var pickupTime = DateTime.UtcNow.AddHours(2);

        // Act
        shipment.SchedulePickup(pickupTime);

        // Assert
        shipment.PickupTime.Should().Be(pickupTime);
        shipment.Status.Should().Be(ShipmentStatus.PickupScheduled);
        shipment.EstimatedDelivery.Should().NotBeNull();
    }

    [Fact(DisplayName = "SchedulePickup: Past time should throw exception")]
    public void SchedulePickup_WithPastTime_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        var pickupTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var act = () => shipment.SchedulePickup(pickupTime);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Pickup time must be in the future*");
    }

    [Fact(DisplayName = "SchedulePickup: From non-pending status should throw exception")]
    public void SchedulePickup_FromPickedUpStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);

        // Act
        var act = () => shipment.SchedulePickup(DateTime.UtcNow.AddHours(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot schedule pickup*");
    }

    [Fact(DisplayName = "MarkAsPickedUp: Should update status and raise event")]
    public void MarkAsPickedUp_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        var pickupTime = DateTime.UtcNow;
        shipment.ClearDomainEvents(); // Clear creation event

        // Act
        shipment.MarkAsPickedUp(pickupTime);

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.PickedUp);
        shipment.PickupTime.Should().Be(pickupTime);
        shipment.EstimatedDelivery.Should().NotBeNull();
        shipment.DomainEvents.Should().HaveCount(1);
        shipment.DomainEvents.First().Should().BeOfType<Shipping.Domain.Events.ShipmentPickedUpEvent>();
    }

    [Theory(DisplayName = "MarkAsPickedUp: Should work from valid statuses")]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.PickupScheduled)]
    public void MarkAsPickedUp_FromValidStatus_ShouldSucceed(ShipmentStatus initialStatus)
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        
        if (initialStatus == ShipmentStatus.PickupScheduled)
            shipment.SchedulePickup(DateTime.UtcNow.AddHours(1));

        // Act
        shipment.MarkAsPickedUp(DateTime.UtcNow);

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.PickedUp);
    }

    #endregion

    #region Transit Tests

    [Fact(DisplayName = "UpdateToInTransit: Should update status and raise events")]
    public void UpdateToInTransit_ShouldUpdateStatusAndRaiseEvents()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.ClearDomainEvents();

        // Act
        shipment.UpdateToInTransit("Memphis Hub", DateTime.UtcNow.AddDays(3));

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.InTransit);
        shipment.DomainEvents.Should().HaveCount(2); // InTransit + StatusChanged events
    }

    [Fact(DisplayName = "UpdateToInTransit: From invalid status should throw exception")]
    public void UpdateToInTransit_FromPendingStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        var act = () => shipment.UpdateToInTransit("Memphis Hub");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot update to in transit*");
    }

    [Fact(DisplayName = "MarkAsOutForDelivery: Should update status and raise events")]
    public void MarkAsOutForDelivery_ShouldUpdateStatusAndRaiseEvents()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.ClearDomainEvents();

        // Act
        shipment.MarkAsOutForDelivery(DateTime.UtcNow.AddHours(4));

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.OutForDelivery);
        shipment.DomainEvents.Should().HaveCount(2);
    }

    [Fact(DisplayName = "MarkAsOutForDelivery: From invalid status should throw exception")]
    public void MarkAsOutForDelivery_FromPendingStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        var act = () => shipment.MarkAsOutForDelivery();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark as out for delivery*");
    }

    #endregion

    #region Delivery Tests

    [Fact(DisplayName = "MarkAsDelivered: Should update status and raise events")]
    public void MarkAsDelivered_WithValidData_ShouldSucceed()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();
        shipment.ClearDomainEvents();

        var deliveryTime = DateTime.UtcNow;
        var recipientName = "John Doe";
        var signatureUrl = "https://example.com/signature.png";

        // Act
        shipment.MarkAsDelivered(deliveryTime, recipientName, signatureUrl);

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.ActualDelivery.Should().Be(deliveryTime);
        shipment.RecipientName.Should().Be(recipientName);
        shipment.SignatureUrl.Should().Be(signatureUrl);
        shipment.DomainEvents.Should().HaveCount(2); // Delivered + StatusChanged
    }

    [Theory(DisplayName = "MarkAsDelivered: Empty recipient name should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsDelivered_WithEmptyRecipientName_ShouldThrowException(string? recipientName)
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();

        // Act
        var act = () => shipment.MarkAsDelivered(DateTime.UtcNow, recipientName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Recipient name is required*");
    }

    [Fact(DisplayName = "MarkDeliveryFailed: First attempt should remain OutForDelivery")]
    public void MarkDeliveryFailed_FirstAttempt_ShouldRemainOutForDelivery()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();

        // Act
        shipment.MarkDeliveryFailed("Customer not available");

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.OutForDelivery);
        shipment.DeliveryAttempts.Should().Be(1);
        shipment.LastDeliveryFailureReason.Should().Be("Customer not available");
    }

    [Fact(DisplayName = "MarkDeliveryFailed: Third attempt should mark as DeliveryFailed")]
    public void MarkDeliveryFailed_ThirdAttempt_ShouldMarkAsDeliveryFailed()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();

        // Act
        shipment.MarkDeliveryFailed("Attempt 1");
        shipment.MarkDeliveryFailed("Attempt 2");
        shipment.MarkDeliveryFailed("Attempt 3");

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.DeliveryFailed);
        shipment.DeliveryAttempts.Should().Be(3);
    }

    [Theory(DisplayName = "MarkDeliveryFailed: Empty reason should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkDeliveryFailed_WithEmptyReason_ShouldThrowException(string? reason)
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();

        // Act
        var act = () => shipment.MarkDeliveryFailed(reason!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Failure reason is required*");
    }

    #endregion

    #region Cancellation and Return Tests

    [Fact(DisplayName = "Cancel: Valid cancellation should succeed")]
    public void Cancel_WithValidReason_ShouldSucceed()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.ClearDomainEvents();

        // Act
        shipment.Cancel("Order cancelled by customer");

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.Cancelled);
        shipment.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Cancel: Delivered shipment should throw exception")]
    public void Cancel_DeliveredShipment_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();
        shipment.MarkAsDelivered(DateTime.UtcNow, "John Doe");

        // Act
        var act = () => shipment.Cancel("Late cancellation");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a delivered shipment*");
    }

    [Fact(DisplayName = "Cancel: Already cancelled shipment should throw exception")]
    public void Cancel_AlreadyCancelled_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.Cancel("First cancellation");

        // Act
        var act = () => shipment.Cancel("Second cancellation");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already cancelled*");
    }

    [Fact(DisplayName = "MarkAsReturned: From delivered status should succeed")]
    public void MarkAsReturned_FromDelivered_ShouldSucceed()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();
        shipment.MarkAsDelivered(DateTime.UtcNow, "John Doe");
        shipment.ClearDomainEvents();

        // Act
        shipment.MarkAsReturned("Customer returned product");

        // Assert
        shipment.Status.Should().Be(ShipmentStatus.Returned);
        shipment.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MarkAsReturned: From invalid status should throw exception")]
    public void MarkAsReturned_FromPendingStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        var act = () => shipment.MarkAsReturned("Invalid return");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark as returned*");
    }

    #endregion

    #region Tracking Tests

    [Fact(DisplayName = "AddTrackingUpdate: Should add to history and raise event")]
    public void AddTrackingUpdate_ShouldAddToHistoryAndRaiseEvent()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.ClearDomainEvents();

        // Act
        shipment.AddTrackingUpdate("Memphis Hub", "In Transit", "Package scanned at hub", 
            DateTime.UtcNow, "35.1495,-90.0490");

        // Assert
        shipment.TrackingHistory.Should().HaveCount(1);
        shipment.TrackingHistory.First().Location.Should().Be("Memphis Hub");
        shipment.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "UpdateCarrierTrackingUrl: Valid URL should update")]
    public void UpdateCarrierTrackingUrl_WithValidUrl_ShouldUpdate()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        shipment.UpdateCarrierTrackingUrl("https://fedex.com/track/TRACK123");

        // Assert
        shipment.CarrierTrackingUrl.Should().Be("https://fedex.com/track/TRACK123");
    }

    [Theory(DisplayName = "UpdateCarrierTrackingUrl: Empty URL should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCarrierTrackingUrl_WithEmptyUrl_ShouldThrowException(string? url)
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        var act = () => shipment.UpdateCarrierTrackingUrl(url!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tracking URL is required*");
    }

    [Fact(DisplayName = "AddNotes: Should update notes")]
    public void AddNotes_ShouldUpdateNotes()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);

        // Act
        shipment.AddNotes("Handle with care - fragile items");

        // Assert
        shipment.Notes.Should().Be("Handle with care - fragile items");
    }

    #endregion

    #region Delay Detection Tests

    [Fact(DisplayName = "IsDelayed: Future estimated delivery should return false")]
    public void IsDelayed_WithFutureEstimatedDelivery_ShouldReturnFalse()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);

        // Act & Assert
        shipment.IsDelayed().Should().BeFalse();
    }

    [Fact(DisplayName = "IsDelayed: Delivered shipment should return false")]
    public void IsDelayed_WhenDelivered_ShouldReturnFalse()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.SameDay, 20m);
        shipment.MarkAsPickedUp(DateTime.UtcNow.AddDays(-2));
        shipment.UpdateToInTransit("Hub");
        shipment.MarkAsOutForDelivery();
        shipment.MarkAsDelivered(DateTime.UtcNow, "John Doe");

        // Act & Assert
        shipment.IsDelayed().Should().BeFalse();
    }

    [Fact(DisplayName = "GetDelayHours: Not delayed should return null")]
    public void GetDelayHours_WhenNotDelayed_ShouldReturnNull()
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), DeliverySpeed.Standard, 10m);
        shipment.MarkAsPickedUp(DateTime.UtcNow);

        // Act & Assert
        shipment.GetDelayHours().Should().BeNull();
    }

    #endregion

    #region Estimated Delivery Tests

    [Theory(DisplayName = "EstimatedDelivery: Should be calculated based on DeliverySpeed")]
    [InlineData(DeliverySpeed.SameDay, 0)]
    [InlineData(DeliverySpeed.Overnight, 1)]
    [InlineData(DeliverySpeed.Express, 2)]
    [InlineData(DeliverySpeed.Standard, 5)]
    public void EstimatedDelivery_ShouldBeCalculatedBasedOnSpeed(DeliverySpeed speed, int expectedDays)
    {
        // Arrange
        var shipment = Shipment.Create(
            Guid.NewGuid(), "TRACK123", Carrier.FedEx, CreateTestAddress(), 
            CreateTestPackage(), speed, 10m);
        var pickupTime = DateTime.UtcNow;

        // Act
        shipment.MarkAsPickedUp(pickupTime);

        // Assert
        shipment.EstimatedDelivery.Should().NotBeNull();
        if (speed == DeliverySpeed.SameDay)
        {
            shipment.EstimatedDelivery.Value.Date.Should().Be(pickupTime.Date);
        }
        else
        {
            var expectedDelivery = pickupTime.AddDays(expectedDays);
            shipment.EstimatedDelivery.Value.Should().BeCloseTo(expectedDelivery, TimeSpan.FromHours(1));
        }
    }

    #endregion
}
