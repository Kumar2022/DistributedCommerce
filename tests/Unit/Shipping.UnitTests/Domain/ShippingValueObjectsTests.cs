using Shipping.Domain.ValueObjects;

namespace Shipping.UnitTests.Domain;

/// <summary>
/// Unit tests for Shipping Value Objects
/// Tests validation, equality, and behavior
/// </summary>
public class ShippingValueObjectsTests
{
    #region ShippingAddress Tests

    [Fact(DisplayName = "ShippingAddress: Valid address should be created")]
    public void ShippingAddress_WithValidData_ShouldBeCreated()
    {
        // Act
        var address = new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA",
            addressLine2: "Apt 4B",
            email: "john@example.com"
        );

        // Assert
        address.Should().NotBeNull();
        address.RecipientName.Should().Be("John Doe");
        address.Phone.Should().Be("+1234567890");
        address.AddressLine1.Should().Be("123 Main St");
        address.AddressLine2.Should().Be("Apt 4B");
        address.City.Should().Be("San Francisco");
        address.StateOrProvince.Should().Be("CA");
        address.PostalCode.Should().Be("94102");
        address.Country.Should().Be("USA");
        address.Email.Should().Be("john@example.com");
    }

    [Theory(DisplayName = "ShippingAddress: Empty recipient name should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShippingAddress_WithEmptyRecipientName_ShouldThrowException(string? recipientName)
    {
        // Act
        var act = () => new ShippingAddress(
            recipientName: recipientName!,
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Recipient name is required*");
    }

    [Theory(DisplayName = "ShippingAddress: Empty address line 1 should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShippingAddress_WithEmptyAddressLine1_ShouldThrowException(string? addressLine1)
    {
        // Act
        var act = () => new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: addressLine1!,
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Address line 1 is required*");
    }

    [Theory(DisplayName = "ShippingAddress: Empty city should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShippingAddress_WithEmptyCity_ShouldThrowException(string? city)
    {
        // Act
        var act = () => new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: city!,
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*City is required*");
    }

    [Theory(DisplayName = "ShippingAddress: Empty postal code should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShippingAddress_WithEmptyPostalCode_ShouldThrowException(string? postalCode)
    {
        // Act
        var act = () => new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: postalCode!,
            country: "USA"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Postal code is required*");
    }

    [Theory(DisplayName = "ShippingAddress: Empty country should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShippingAddress_WithEmptyCountry_ShouldThrowException(string? country)
    {
        // Act
        var act = () => new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: country!
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Country is required*");
    }

    [Fact(DisplayName = "ShippingAddress: GetFullAddress should format correctly without address line 2")]
    public void ShippingAddress_GetFullAddress_WithoutAddressLine2_ShouldFormatCorrectly()
    {
        // Arrange
        var address = new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA"
        );

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St, San Francisco, CA 94102, USA");
    }

    [Fact(DisplayName = "ShippingAddress: GetFullAddress should include address line 2")]
    public void ShippingAddress_GetFullAddress_WithAddressLine2_ShouldIncludeIt()
    {
        // Arrange
        var address = new ShippingAddress(
            recipientName: "John Doe",
            phone: "+1234567890",
            addressLine1: "123 Main St",
            city: "San Francisco",
            stateOrProvince: "CA",
            postalCode: "94102",
            country: "USA",
            addressLine2: "Apt 4B"
        );

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St, Apt 4B, San Francisco, CA 94102, USA");
    }

    [Fact(DisplayName = "ShippingAddress: Same values should be equal")]
    public void ShippingAddress_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new ShippingAddress(
            "John Doe", "+1234567890", "123 Main St", "SF", "CA", "94102", "USA");
        var address2 = new ShippingAddress(
            "John Doe", "+1234567890", "123 Main St", "SF", "CA", "94102", "USA");

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact(DisplayName = "ShippingAddress: Different values should not be equal")]
    public void ShippingAddress_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new ShippingAddress(
            "John Doe", "+1234567890", "123 Main St", "SF", "CA", "94102", "USA");
        var address2 = new ShippingAddress(
            "Jane Doe", "+1234567890", "123 Main St", "SF", "CA", "94102", "USA");

        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    #endregion

    #region Package Tests

    [Fact(DisplayName = "Package: Valid package should be created")]
    public void Package_WithValidDimensions_ShouldBeCreated()
    {
        // Act
        var package = new Package(
            weight: 2.5m,
            length: 30,
            width: 20,
            height: 15
        );

        // Assert
        package.Should().NotBeNull();
        package.Weight.Should().Be(2.5m);
        package.Length.Should().Be(30);
        package.Width.Should().Be(20);
        package.Height.Should().Be(15);
        package.WeightUnit.Should().Be("kg");
        package.DimensionUnit.Should().Be("cm");
    }

    [Theory(DisplayName = "Package: Zero or negative weight should throw exception")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Package_WithInvalidWeight_ShouldThrowException(decimal weight)
    {
        // Act
        var act = () => new Package(weight, 30, 20, 15);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Weight must be positive*");
    }

    [Theory(DisplayName = "Package: Zero or negative dimensions should throw exception")]
    [InlineData(0, 20, 15)]
    [InlineData(30, 0, 15)]
    [InlineData(30, 20, 0)]
    [InlineData(-10, 20, 15)]
    public void Package_WithInvalidDimensions_ShouldThrowException(decimal length, decimal width, decimal height)
    {
        // Act
        var act = () => new Package(2.5m, length, width, height);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Dimensions must be positive*");
    }

    [Fact(DisplayName = "Package: GetVolume should calculate correctly")]
    public void Package_GetVolume_ShouldCalculateCorrectly()
    {
        // Arrange
        var package = new Package(2.5m, 30, 20, 15);

        // Act
        var volume = package.GetVolume();

        // Assert
        volume.Should().Be(9000); // 30 * 20 * 15
    }

    [Fact(DisplayName = "Package: GetVolumetricWeight should calculate correctly")]
    public void Package_GetVolumetricWeight_ShouldCalculateCorrectly()
    {
        // Arrange
        var package = new Package(2.5m, 30, 20, 15);

        // Act
        var volumetricWeight = package.GetVolumetricWeight();

        // Assert
        volumetricWeight.Should().Be(1.8m); // 9000 / 5000
    }

    [Fact(DisplayName = "Package: Same values should be equal")]
    public void Package_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var package1 = new Package(2.5m, 30, 20, 15);
        var package2 = new Package(2.5m, 30, 20, 15);

        // Assert
        package1.Should().Be(package2);
    }

    [Fact(DisplayName = "Package: Different values should not be equal")]
    public void Package_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var package1 = new Package(2.5m, 30, 20, 15);
        var package2 = new Package(3.0m, 30, 20, 15);

        // Assert
        package1.Should().NotBe(package2);
    }

    #endregion

    #region TrackingInfo Tests

    [Fact(DisplayName = "TrackingInfo: Valid info should be created")]
    public void TrackingInfo_WithValidData_ShouldBeCreated()
    {
        // Act
        var trackingInfo = new TrackingInfo(
            location: "Memphis Hub",
            status: "In Transit",
            description: "Package scanned at sorting facility",
            timestamp: DateTime.UtcNow,
            coordinates: "35.1495,-90.0490"
        );

        // Assert
        trackingInfo.Should().NotBeNull();
        trackingInfo.Location.Should().Be("Memphis Hub");
        trackingInfo.Status.Should().Be("In Transit");
        trackingInfo.Description.Should().Be("Package scanned at sorting facility");
        trackingInfo.Coordinates.Should().Be("35.1495,-90.0490");
    }

    [Theory(DisplayName = "TrackingInfo: Empty location should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TrackingInfo_WithEmptyLocation_ShouldThrowException(string? location)
    {
        // Act
        var act = () => new TrackingInfo(
            location: location!,
            status: "In Transit",
            description: "Test",
            timestamp: DateTime.UtcNow
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Location is required*");
    }

    [Theory(DisplayName = "TrackingInfo: Empty status should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TrackingInfo_WithEmptyStatus_ShouldThrowException(string? status)
    {
        // Act
        var act = () => new TrackingInfo(
            location: "Memphis Hub",
            status: status!,
            description: "Test",
            timestamp: DateTime.UtcNow
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Status is required*");
    }

    [Fact(DisplayName = "TrackingInfo: Coordinates can be null")]
    public void TrackingInfo_WithNullCoordinates_ShouldSucceed()
    {
        // Act
        var trackingInfo = new TrackingInfo(
            location: "Memphis Hub",
            status: "In Transit",
            description: "Test",
            timestamp: DateTime.UtcNow,
            coordinates: null
        );

        // Assert
        trackingInfo.Coordinates.Should().BeNull();
    }

    [Fact(DisplayName = "TrackingInfo: Same values should be equal")]
    public void TrackingInfo_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var info1 = new TrackingInfo("Memphis Hub", "In Transit", "Scanned", timestamp);
        var info2 = new TrackingInfo("Memphis Hub", "In Transit", "Scanned", timestamp);

        // Assert
        info1.Should().Be(info2);
    }

    #endregion
}
