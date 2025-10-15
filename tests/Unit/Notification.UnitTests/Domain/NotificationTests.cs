using Notification.Domain.Exceptions;
using Notification.Domain.ValueObjects;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.UnitTests.Domain;

/// <summary>
/// Unit tests for Notification Aggregate Root
/// Tests notification lifecycle, state transitions, and business rules
/// </summary>
public class NotificationTests
{
    private static Recipient CreateTestRecipient()
    {
        return Recipient.Create(
            userId: Guid.NewGuid(),
            email: "test@example.com",
            name: "Test User",
            phoneNumber: "+1234567890",
            deviceToken: "device-token-123"
        );
    }

    private static NotificationContent CreateTestContent()
    {
        return NotificationContent.Create(
            subject: "Test Subject",
            body: "Test Body",
            variables: new Dictionary<string, string> { { "name", "John" } }
        );
    }

    #region Creation Tests

    [Fact(DisplayName = "Create: Valid notification should succeed")]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var recipient = CreateTestRecipient();
        var content = CreateTestContent();

        // Act
        var notification = NotificationAggregate.Create(
            recipient, NotificationChannel.Email, content);

        // Assert
        notification.Should().NotBeNull();
        notification.Id.Should().NotBeEmpty();
        notification.Recipient.Should().Be(recipient);
        notification.Channel.Should().Be(NotificationChannel.Email);
        notification.Content.Should().Be(content);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.Priority.Should().Be(NotificationPriority.Normal);
        notification.RetryCount.Should().Be(0);
        notification.MaxRetries.Should().Be(3);
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Create: Null recipient should throw exception")]
    public void Create_WithNullRecipient_ShouldThrowException()
    {
        // Arrange
        var content = CreateTestContent();

        // Act
        var act = () => NotificationAggregate.Create(
            null!, NotificationChannel.Email, content);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create: Null content should throw exception")]
    public void Create_WithNullContent_ShouldThrowException()
    {
        // Arrange
        var recipient = CreateTestRecipient();

        // Act
        var act = () => NotificationAggregate.Create(
            recipient, NotificationChannel.Email, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Create: Email channel without email should throw exception")]
    public void Create_EmailChannel_WithoutEmail_ShouldThrowException()
    {
        // Arrange
        var recipient = Recipient.Create(
            Guid.NewGuid(), "test@example.com", "Test", phoneNumber: "+123");
        var recipientNoEmail = recipient with { Email = "" };
        var content = CreateTestContent();

        // Act
        var act = () => NotificationAggregate.Create(
            recipientNoEmail, NotificationChannel.Email, content);

        // Assert
        act.Should().Throw<InvalidRecipientException>()
            .WithMessage("*Email is required for email notifications*");
    }

    [Fact(DisplayName = "Create: SMS channel without phone should throw exception")]
    public void Create_SmsChannel_WithoutPhone_ShouldThrowException()
    {
        // Arrange
        var recipient = Recipient.Create(
            Guid.NewGuid(), "test@example.com", "Test");
        var content = CreateTestContent();

        // Act
        var act = () => NotificationAggregate.Create(
            recipient, NotificationChannel.SMS, content);

        // Assert
        act.Should().Throw<InvalidRecipientException>()
            .WithMessage("*Phone number is required for SMS notifications*");
    }

    [Fact(DisplayName = "Create: Push channel without device token should throw exception")]
    public void Create_PushChannel_WithoutDeviceToken_ShouldThrowException()
    {
        // Arrange
        var recipient = Recipient.Create(
            Guid.NewGuid(), "test@example.com", "Test");
        var content = CreateTestContent();

        // Act
        var act = () => NotificationAggregate.Create(
            recipient, NotificationChannel.Push, content);

        // Assert
        act.Should().Throw<InvalidRecipientException>()
            .WithMessage("*Device token is required for push notifications*");
    }

    [Fact(DisplayName = "Create: With custom priority should set priority")]
    public void Create_WithCustomPriority_ShouldSetPriority()
    {
        // Act
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent(),
            priority: NotificationPriority.Urgent);

        // Assert
        notification.Priority.Should().Be(NotificationPriority.Urgent);
    }

    [Fact(DisplayName = "Create: With scheduled time should set scheduled time")]
    public void Create_WithScheduledTime_ShouldSetScheduledTime()
    {
        // Arrange
        var scheduledFor = DateTime.UtcNow.AddHours(1);

        // Act
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent(),
            scheduledFor: scheduledFor);

        // Assert
        notification.ScheduledFor.Should().Be(scheduledFor);
    }

    [Fact(DisplayName = "Create: Should raise NotificationCreatedEvent")]
    public void Create_ShouldRaiseNotificationCreatedEvent()
    {
        // Act
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());

        // Assert
        notification.DomainEvents.Should().HaveCount(1);
        notification.DomainEvents.First().Should().BeOfType<Notification.Domain.Events.NotificationCreatedEvent>();
    }

    #endregion

    #region Status Transition Tests

    [Fact(DisplayName = "MarkAsSent: From Pending should succeed")]
    public void MarkAsSent_FromPending_ShouldSucceed()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.ClearDomainEvents();

        // Act
        notification.MarkAsSent("ext-123");

        // Assert
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
        notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.ExternalId.Should().Be("ext-123");
        notification.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MarkAsSent: From non-Pending status should throw exception")]
    public void MarkAsSent_FromSentStatus_ShouldThrowException()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.MarkAsSent();

        // Act
        var act = () => notification.MarkAsSent();

        // Assert
        act.Should().Throw<InvalidNotificationStateException>()
            .WithMessage("*Cannot mark notification as sent*");
    }

    [Fact(DisplayName = "MarkAsDelivered: From Sent should succeed")]
    public void MarkAsDelivered_FromSent_ShouldSucceed()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.MarkAsSent();
        notification.ClearDomainEvents();

        // Act
        notification.MarkAsDelivered("ext-456");

        // Assert
        notification.Status.Should().Be(NotificationStatus.Delivered);
        notification.DeliveredAt.Should().NotBeNull();
        notification.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.ExternalId.Should().Be("ext-456");
        notification.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MarkAsDelivered: From non-Sent status should throw exception")]
    public void MarkAsDelivered_FromPendingStatus_ShouldThrowException()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());

        // Act
        var act = () => notification.MarkAsDelivered();

        // Assert
        act.Should().Throw<InvalidNotificationStateException>()
            .WithMessage("*Cannot mark notification as delivered*");
    }

    [Fact(DisplayName = "MarkAsFailed: Should update status and increment retry count")]
    public void MarkAsFailed_ShouldUpdateStatusAndIncrementRetryCount()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.ClearDomainEvents();

        // Act
        notification.MarkAsFailed("Connection timeout");

        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailedAt.Should().NotBeNull();
        notification.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.ErrorMessage.Should().Be("Connection timeout");
        notification.RetryCount.Should().Be(1);
        notification.DomainEvents.Should().HaveCount(1);
    }

    [Theory(DisplayName = "MarkAsFailed: Empty error message should throw exception")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsFailed_WithEmptyErrorMessage_ShouldThrowException(string? errorMessage)
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());

        // Act
        var act = () => notification.MarkAsFailed(errorMessage!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Error message is required*");
    }

    #endregion

    #region Cancellation Tests

    [Fact(DisplayName = "Cancel: Valid cancellation should succeed")]
    public void Cancel_WithValidReason_ShouldSucceed()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.ClearDomainEvents();

        // Act
        notification.Cancel("User requested cancellation");

        // Assert
        notification.Status.Should().Be(NotificationStatus.Cancelled);
        notification.CancelledAt.Should().NotBeNull();
        notification.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.CancellationReason.Should().Be("User requested cancellation");
        notification.DomainEvents.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Cancel: Delivered notification should throw exception")]
    public void Cancel_DeliveredNotification_ShouldThrowException()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.MarkAsSent();
        notification.MarkAsDelivered();

        // Act
        var act = () => notification.Cancel("Late cancellation");

        // Assert
        act.Should().Throw<InvalidNotificationStateException>()
            .WithMessage("*Cannot cancel an already delivered notification*");
    }

    [Fact(DisplayName = "Cancel: Already cancelled notification should throw exception")]
    public void Cancel_AlreadyCancelled_ShouldThrowException()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.Cancel("First cancellation");

        // Act
        var act = () => notification.Cancel("Second cancellation");

        // Assert
        act.Should().Throw<InvalidNotificationStateException>()
            .WithMessage("*already cancelled*");
    }

    #endregion

    #region Retry Logic Tests

    [Fact(DisplayName = "CanRetry: Failed with retries remaining should return true")]
    public void CanRetry_FailedWithRetriesRemaining_ShouldReturnTrue()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent(),
            maxRetries: 3);
        notification.MarkAsFailed("First failure");

        // Act & Assert
        notification.CanRetry().Should().BeTrue();
        notification.RetryCount.Should().Be(1);
        notification.MaxRetries.Should().Be(3);
    }

    [Fact(DisplayName = "CanRetry: Failed with max retries reached should return false")]
    public void CanRetry_FailedWithMaxRetries_ShouldReturnFalse()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent(),
            maxRetries: 2);
        notification.MarkAsFailed("First failure");
        notification.ResetForRetry();
        notification.MarkAsFailed("Second failure");

        // Act & Assert - After 2 failures (with maxRetries=2), CanRetry should be false
        notification.CanRetry().Should().BeFalse();
        notification.RetryCount.Should().Be(2);
        notification.MaxRetries.Should().Be(2);
        
        // ResetForRetry should throw exception
        var act = () => notification.ResetForRetry();
        act.Should().Throw<Notification.Domain.Exceptions.InvalidNotificationStateException>();
    }

    [Fact(DisplayName = "CanRetry: Non-failed status should return false")]
    public void CanRetry_PendingStatus_ShouldReturnFalse()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());

        // Act & Assert
        notification.CanRetry().Should().BeFalse();
    }

    [Fact(DisplayName = "ResetForRetry: Should reset to Pending status")]
    public void ResetForRetry_ShouldResetToPendingStatus()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());
        notification.MarkAsFailed("First failure");

        // Act
        notification.ResetForRetry();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.ErrorMessage.Should().BeNull();
        notification.FailedAt.Should().BeNull();
        notification.RetryCount.Should().Be(1); // Retry count stays to track attempts
    }

    [Fact(DisplayName = "ResetForRetry: When cannot retry should throw exception")]
    public void ResetForRetry_WhenCannotRetry_ShouldThrowException()
    {
        // Arrange
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent());

        // Act
        var act = () => notification.ResetForRetry();

        // Assert
        act.Should().Throw<InvalidNotificationStateException>()
            .WithMessage("*Cannot retry*");
    }

    [Fact(DisplayName = "Retry workflow: Multiple retries should work correctly")]
    public void RetryWorkflow_MultipleRetries_ShouldWorkCorrectly()
    {
        // Arrange - MaxRetries=3 means we can retry up to 3 times (4 total attempts)
        var notification = NotificationAggregate.Create(
            CreateTestRecipient(), NotificationChannel.Email, CreateTestContent(),
            maxRetries: 3);

        // Act - First attempt fails (RetryCount=1)
        notification.MarkAsFailed("Attempt 1 failed");
        notification.RetryCount.Should().Be(1);
        notification.CanRetry().Should().BeTrue(); // 1 < 3
        notification.ResetForRetry();

        // Act - Second attempt fails (RetryCount=2)
        notification.MarkAsFailed("Attempt 2 failed");
        notification.RetryCount.Should().Be(2);
        notification.CanRetry().Should().BeTrue(); // 2 < 3
        notification.ResetForRetry();

        // Act - Third attempt fails (RetryCount=3)
        notification.MarkAsFailed("Attempt 3 failed");
        notification.RetryCount.Should().Be(3);
        notification.CanRetry().Should().BeFalse(); // 3 < 3 is false, max retries reached

        // Assert - Cannot retry anymore
        notification.Status.Should().Be(NotificationStatus.Failed);
        var act = () => notification.ResetForRetry();
        act.Should().Throw<Notification.Domain.Exceptions.InvalidNotificationStateException>();
    }

    #endregion
}
