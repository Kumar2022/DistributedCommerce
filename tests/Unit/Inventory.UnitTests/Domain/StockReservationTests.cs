using Inventory.Domain.Aggregates;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Unit tests for StockReservation entity
/// Tests reservation lifecycle and expiration
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Domain")]
public class StockReservationTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public StockReservationTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
    }

    #region Creation Tests

    [Fact(DisplayName = "Create Reservation: With valid data should succeed")]
    public void CreateReservation_WithValidData_ShouldSucceed()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var quantity = 10;
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // Act
        var reservation = StockReservation.Create(reservationId, productId, orderId, quantity, expiresAt);

        // Assert
        reservation.Should().NotBeNull();
        reservation.ReservationId.Should().Be(reservationId);
        reservation.ProductId.Should().Be(productId);
        reservation.OrderId.Should().Be(orderId);
        reservation.Quantity.Should().Be(quantity);
        reservation.ExpiresAt.Should().Be(expiresAt);
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.ReservedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        reservation.ConfirmedAt.Should().BeNull();
        reservation.ReleasedAt.Should().BeNull();
    }

    #endregion

    #region Expiration Tests

    [Fact(DisplayName = "Is Expired: Future expiration should return false")]
    public void IsExpired_FutureExpiration_ShouldReturnFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var reservation = CreateValidReservation(expiresAt);

        // Act
        var isExpired = reservation.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact(DisplayName = "Is Expired: Past expiration should return true")]
    public void IsExpired_PastExpiration_ShouldReturnTrue()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);
        var reservation = CreateValidReservation(expiresAt);

        // Act
        var isExpired = reservation.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact(DisplayName = "Is Expired: Confirmed reservation should return false")]
    public void IsExpired_ConfirmedReservation_ShouldReturnFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(-1);
        var reservation = CreateValidReservation(expiresAt);
        reservation.Confirm();

        // Act
        var isExpired = reservation.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    #endregion

    #region Status Transition Tests

    [Fact(DisplayName = "Confirm: Should update status and set confirmed date")]
    public void Confirm_ShouldUpdateStatusAndSetConfirmedDate()
    {
        // Arrange
        var reservation = CreateValidReservation();
        reservation.Status.Should().Be(ReservationStatus.Active);

        // Act
        reservation.Confirm();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ConfirmedAt.Should().NotBeNull();
        reservation.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Release: Should update status and set released date")]
    public void Release_ShouldUpdateStatusAndSetReleasedDate()
    {
        // Arrange
        var reservation = CreateValidReservation();
        reservation.Status.Should().Be(ReservationStatus.Active);

        // Act
        reservation.Release();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.ReleasedAt.Should().NotBeNull();
        reservation.ReleasedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Expire: Should update status and set released date")]
    public void Expire_ShouldUpdateStatusAndSetReleasedDate()
    {
        // Arrange
        var reservation = CreateValidReservation();
        reservation.Status.Should().Be(ReservationStatus.Active);

        // Act
        reservation.Expire();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.ReleasedAt.Should().NotBeNull();
        reservation.ReleasedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Reservation Lifecycle Tests

    [Fact(DisplayName = "Reservation Lifecycle: From active to confirmed")]
    public void ReservationLifecycle_FromActiveToConfirmed()
    {
        // Arrange
        var reservation = CreateValidReservation();

        // Act & Assert
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.ConfirmedAt.Should().BeNull();

        reservation.Confirm();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ConfirmedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Reservation Lifecycle: From active to released")]
    public void ReservationLifecycle_FromActiveToReleased()
    {
        // Arrange
        var reservation = CreateValidReservation();

        // Act & Assert
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.ReleasedAt.Should().BeNull();

        reservation.Release();
        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.ReleasedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "Reservation Lifecycle: From active to expired")]
    public void ReservationLifecycle_FromActiveToExpired()
    {
        // Arrange
        var reservation = CreateValidReservation();

        // Act & Assert
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.ReleasedAt.Should().BeNull();

        reservation.Expire();
        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.ReleasedAt.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private StockReservation CreateValidReservation(DateTime? expiresAt = null)
    {
        var expiration = expiresAt ?? DateTime.UtcNow.AddMinutes(15);
        return StockReservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            expiration);
    }

    #endregion
}
