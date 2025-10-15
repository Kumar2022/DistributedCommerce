using Notification.Domain.Events;
using Notification.Domain.Exceptions;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Aggregates;

/// <summary>
/// Notification Aggregate Root
/// Represents a single notification sent to a user via a specific channel
/// </summary>
public class Notification : AggregateRoot<Guid>
{
    public Recipient Recipient { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationContent Content { get; private set; }
    public NotificationStatus Status { get; private set; }
    public NotificationPriority Priority { get; private set; }
    
    public Guid? TemplateId { get; private set; }
    public string? ExternalId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    public DateTime? ScheduledFor { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private Notification() 
    {
        Recipient = null!;
        Content = null!;
    }

    private Notification(
        Guid id,
        Recipient recipient,
        NotificationChannel channel,
        NotificationContent content,
        NotificationPriority priority,
        Guid? templateId,
        DateTime? scheduledFor,
        int maxRetries = 3)
    {
        Id = id;
        Recipient = recipient;
        Channel = channel;
        Content = content;
        Priority = priority;
        TemplateId = templateId;
        ScheduledFor = scheduledFor;
        MaxRetries = maxRetries;
        
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;

        AddDomainEvent(new NotificationCreatedEvent(
            Id,
            recipient.UserId,
            channel,
            content.Subject,
            CreatedAt
        ));
    }

    public static Notification Create(
        Recipient recipient,
        NotificationChannel channel,
        NotificationContent content,
        NotificationPriority priority = NotificationPriority.Normal,
        Guid? templateId = null,
        DateTime? scheduledFor = null,
        int maxRetries = 3)
    {
        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        ValidateChannelForRecipient(channel, recipient);

        return new Notification(
            Guid.NewGuid(),
            recipient,
            channel,
            content,
            priority,
            templateId,
            scheduledFor,
            maxRetries
        );
    }

    public void MarkAsSent(string? externalId = null)
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidNotificationStateException(
                $"Cannot mark notification as sent. Current status: {Status}");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalId = externalId;

        AddDomainEvent(new NotificationSentEvent(
            Id,
            Recipient.UserId,
            Channel,
            SentAt.Value,
            externalId
        ));
    }

    public void MarkAsDelivered(string? externalId = null)
    {
        if (Status != NotificationStatus.Sent)
            throw new InvalidNotificationStateException(
                $"Cannot mark notification as delivered. Current status: {Status}");

        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        if (externalId != null)
            ExternalId = externalId;

        AddDomainEvent(new NotificationDeliveredEvent(
            Id,
            Recipient.UserId,
            Channel,
            DeliveredAt.Value,
            ExternalId
        ));
    }

    public void MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required", nameof(errorMessage));

        Status = NotificationStatus.Failed;
        FailedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;

        AddDomainEvent(new NotificationFailedEvent(
            Id,
            Recipient.UserId,
            Channel,
            errorMessage,
            FailedAt.Value,
            RetryCount
        ));
    }

    public void Cancel(string reason)
    {
        if (Status == NotificationStatus.Delivered)
            throw new InvalidNotificationStateException(
                "Cannot cancel an already delivered notification");

        if (Status == NotificationStatus.Cancelled)
            throw new InvalidNotificationStateException(
                "Notification is already cancelled");

        Status = NotificationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        AddDomainEvent(new NotificationCancelledEvent(
            Id,
            Recipient.UserId,
            reason,
            CancelledAt.Value
        ));
    }

    public bool CanRetry()
    {
        return Status == NotificationStatus.Failed && RetryCount < MaxRetries;
    }

    public void ResetForRetry()
    {
        if (!CanRetry())
            throw new InvalidNotificationStateException(
                $"Cannot retry. Status: {Status}, RetryCount: {RetryCount}, MaxRetries: {MaxRetries}");

        Status = NotificationStatus.Pending;
        ErrorMessage = null;
        FailedAt = null;
    }

    private static void ValidateChannelForRecipient(NotificationChannel channel, Recipient recipient)
    {
        switch (channel)
        {
            case NotificationChannel.Email:
                if (string.IsNullOrWhiteSpace(recipient.Email))
                    throw new InvalidRecipientException("Email is required for email notifications");
                break;
            
            case NotificationChannel.SMS:
                if (string.IsNullOrWhiteSpace(recipient.PhoneNumber))
                    throw new InvalidRecipientException("Phone number is required for SMS notifications");
                break;
            
            case NotificationChannel.Push:
                if (string.IsNullOrWhiteSpace(recipient.DeviceToken))
                    throw new InvalidRecipientException("Device token is required for push notifications");
                break;
        }
    }
}
