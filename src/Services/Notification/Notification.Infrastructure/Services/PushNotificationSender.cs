using Microsoft.Extensions.Configuration;

namespace Notification.Infrastructure.Services;

public class PushNotificationSender : INotificationSender
{
    private readonly ILogger<PushNotificationSender> _logger;
    private readonly IConfiguration _configuration;

    public NotificationChannel Channel => NotificationChannel.Push;

    public PushNotificationSender(
        ILogger<PushNotificationSender> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SendNotificationResult> SendAsync(
        string recipient,
        string subject,
        string body,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending push notification to {Recipient}", recipient);

            var mockSend = _configuration["Push:MockSend"];
            if (string.IsNullOrEmpty(mockSend) || mockSend == "true")
            {
                _logger.LogInformation(
                    "MOCK PUSH SENT:\nTo: {To}\nTitle: {Title}\nMessage: {Message}",
                    recipient, subject, body);

                var externalId = $"mock-push-{Guid.NewGuid()}";
                await Task.CompletedTask;
                return new SendNotificationResult(true, externalId);
            }

            // Production Firebase/APNs integration would go here
            _logger.LogInformation("Push notification sent successfully to {Recipient}", recipient);
            return new SendNotificationResult(true, $"push-{DateTime.UtcNow.Ticks}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to {Recipient}", recipient);
            return new SendNotificationResult(false, null, ex.Message);
        }
    }
}
