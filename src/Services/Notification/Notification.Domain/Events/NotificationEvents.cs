using Notification.Domain.ValueObjects;

namespace Notification.Domain.Events;

/// <summary>
/// Raised when a notification is created
/// </summary>
public record NotificationCreatedEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    string Subject,
    DateTime CreatedAt
) : DomainEvent;

/// <summary>
/// Raised when a notification is sent
/// </summary>
public record NotificationSentEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    DateTime SentAt,
    string? ExternalId
) : DomainEvent;

/// <summary>
/// Raised when a notification is delivered
/// </summary>
public record NotificationDeliveredEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    DateTime DeliveredAt,
    string? ExternalId
) : DomainEvent;

/// <summary>
/// Raised when a notification fails to send
/// </summary>
public record NotificationFailedEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    string ErrorMessage,
    DateTime FailedAt,
    int RetryCount
) : DomainEvent;

/// <summary>
/// Raised when a notification is cancelled
/// </summary>
public record NotificationCancelledEvent(
    Guid NotificationId,
    Guid UserId,
    string Reason,
    DateTime CancelledAt
) : DomainEvent;

/// <summary>
/// Raised when a notification template is created
/// </summary>
public record TemplateCreatedEvent(
    Guid TemplateId,
    string TemplateName,
    NotificationChannel Channel,
    DateTime CreatedAt
) : DomainEvent;

/// <summary>
/// Raised when a notification template is updated
/// </summary>
public record TemplateUpdatedEvent(
    Guid TemplateId,
    string TemplateName,
    DateTime UpdatedAt
) : DomainEvent;

/// <summary>
/// Raised when a notification template is activated/deactivated
/// </summary>
public record TemplateStatusChangedEvent(
    Guid TemplateId,
    bool IsActive,
    DateTime ChangedAt
) : DomainEvent;
