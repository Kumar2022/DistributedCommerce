using Notification.Domain.ValueObjects;

namespace Notification.UnitTests.Domain;

/// <summary>
/// Unit tests for Notification Value Objects
/// Tests validation, equality, and factory methods
/// </summary>
public class NotificationValueObjectsTests
{
    #region Recipient Tests

    [Fact(DisplayName = "Recipient: Valid recipient should be created")]
    public void Recipient_WithValidData_ShouldBeCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var phone = "+1234567890";
        var deviceToken = "device-token-123";
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        var recipient = Recipient.Create(userId, email, name, phone, deviceToken, metadata);

        // Assert
        recipient.Should().NotBeNull();
        recipient.UserId.Should().Be(userId);
        recipient.Email.Should().Be(email);
        recipient.Name.Should().Be(name);
        recipient.PhoneNumber.Should().Be(phone);
        recipient.DeviceToken.Should().Be(deviceToken);
        recipient.Metadata.Should().ContainKey("key");
        recipient.Metadata["key"].Should().Be("value");
    }

    [Fact(DisplayName = "Recipient: Empty user ID should throw exception")]
    public void Recipient_WithEmptyUserId_ShouldThrowException()
    {
        // Act
        var act = () => Recipient.Create(
            userId: Guid.Empty,
            email: "test@example.com",
            name: "Test User"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID is required*");
    }

    [Theory(DisplayName = "Recipient: Empty email should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recipient_WithEmptyEmail_ShouldThrowException(string? email)
    {
        // Act
        var act = () => Recipient.Create(
            userId: Guid.NewGuid(),
            email: email!,
            name: "Test User"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email is required*");
    }

    [Theory(DisplayName = "Recipient: Empty name should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recipient_WithEmptyName_ShouldThrowException(string? name)
    {
        // Act
        var act = () => Recipient.Create(
            userId: Guid.NewGuid(),
            email: "test@example.com",
            name: name!
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Name is required*");
    }

    [Fact(DisplayName = "Recipient: Optional fields can be null")]
    public void Recipient_WithOptionalFieldsNull_ShouldSucceed()
    {
        // Act
        var recipient = Recipient.Create(
            userId: Guid.NewGuid(),
            email: "test@example.com",
            name: "Test User"
        );

        // Assert
        recipient.PhoneNumber.Should().BeNull();
        recipient.DeviceToken.Should().BeNull();
        recipient.Metadata.Should().NotBeNull();
        recipient.Metadata.Should().BeEmpty();
    }

    [Fact(DisplayName = "Recipient: Same values should be equal")]
    public void Recipient_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipient1 = Recipient.Create(userId, "test@example.com", "Test");
        var recipient2 = Recipient.Create(userId, "test@example.com", "Test");

        // Assert - Use BeEquivalentTo for value objects with collections
        recipient1.Should().BeEquivalentTo(recipient2);
        recipient1.UserId.Should().Be(recipient2.UserId);
        recipient1.Email.Should().Be(recipient2.Email);
        recipient1.Name.Should().Be(recipient2.Name);
    }

    #endregion

    #region NotificationContent Tests

    [Fact(DisplayName = "NotificationContent: Valid content should be created")]
    public void NotificationContent_WithValidData_ShouldBeCreated()
    {
        // Arrange
        var subject = "Test Subject";
        var body = "Test Body";
        var variables = new Dictionary<string, string> { { "name", "John" }, { "count", "5" } };
        var metadata = new Dictionary<string, string> { { "priority", "high" } };

        // Act
        var content = NotificationContent.Create(subject, body, variables, metadata);

        // Assert
        content.Should().NotBeNull();
        content.Subject.Should().Be(subject);
        content.Body.Should().Be(body);
        content.Variables.Should().ContainKey("name");
        content.Variables["name"].Should().Be("John");
        content.Variables.Should().ContainKey("count");
        content.Metadata.Should().ContainKey("priority");
        content.Metadata["priority"].Should().Be("high");
    }

    [Theory(DisplayName = "NotificationContent: Empty subject should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotificationContent_WithEmptySubject_ShouldThrowException(string? subject)
    {
        // Act
        var act = () => NotificationContent.Create(
            subject: subject!,
            body: "Test Body"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Subject is required*");
    }

    [Theory(DisplayName = "NotificationContent: Empty body should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotificationContent_WithEmptyBody_ShouldThrowException(string? body)
    {
        // Act
        var act = () => NotificationContent.Create(
            subject: "Test Subject",
            body: body!
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Body is required*");
    }

    [Fact(DisplayName = "NotificationContent: Optional dictionaries can be null")]
    public void NotificationContent_WithNullDictionaries_ShouldSucceed()
    {
        // Act
        var content = NotificationContent.Create(
            subject: "Test",
            body: "Body"
        );

        // Assert
        content.Variables.Should().NotBeNull();
        content.Variables.Should().BeEmpty();
        content.Metadata.Should().NotBeNull();
        content.Metadata.Should().BeEmpty();
    }

    [Fact(DisplayName = "NotificationContent: Same values should be equal")]
    public void NotificationContent_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var content1 = NotificationContent.Create("Subject", "Body");
        var content2 = NotificationContent.Create("Subject", "Body");

        // Assert - Use BeEquivalentTo for value objects with collections
        content1.Should().BeEquivalentTo(content2);
        content1.Subject.Should().Be(content2.Subject);
        content1.Body.Should().Be(content2.Body);
    }

    #endregion

    #region DeliveryResult Tests

    [Fact(DisplayName = "DeliveryResult: Success should create success result")]
    public void DeliveryResult_Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = DeliveryResult.Success("ext-123", new Dictionary<string, string> { { "key", "value" } });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ExternalId.Should().Be("ext-123");
        result.DeliveredAt.Should().NotBeNull();
        result.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().ContainKey("key");
    }

    [Fact(DisplayName = "DeliveryResult: Success without external ID should work")]
    public void DeliveryResult_SuccessWithoutExternalId_ShouldWork()
    {
        // Act
        var result = DeliveryResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExternalId.Should().BeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Fact(DisplayName = "DeliveryResult: Failure should create failure result")]
    public void DeliveryResult_Failure_ShouldCreateFailureResult()
    {
        // Act
        var result = DeliveryResult.Failure("Connection timeout", 
            new Dictionary<string, string> { { "attempts", "3" } });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection timeout");
        result.ExternalId.Should().BeNull();
        result.DeliveredAt.Should().BeNull();
        result.Metadata.Should().ContainKey("attempts");
        result.Metadata["attempts"].Should().Be("3");
    }

    [Fact(DisplayName = "DeliveryResult: Failure without metadata should work")]
    public void DeliveryResult_FailureWithoutMetadata_ShouldWork()
    {
        // Act
        var result = DeliveryResult.Failure("Error occurred");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error occurred");
        result.Metadata.Should().BeEmpty();
    }

    #endregion
}
