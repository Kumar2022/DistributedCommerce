namespace Notification.Infrastructure.Services;

public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task<SendNotificationResult> SendAsync(
        string recipient, 
        string subject, 
        string body, 
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}

public record SendNotificationResult(
    bool Success,
    string? ExternalId = null,
    string? ErrorMessage = null);
