using Microsoft.Extensions.Configuration;

namespace Notification.Infrastructure.Services;

public class SmsNotificationSender : INotificationSender
{
    private readonly ILogger<SmsNotificationSender> _logger;
    private readonly IConfiguration _configuration;

    public NotificationChannel Channel => NotificationChannel.SMS;

    public SmsNotificationSender(
        ILogger<SmsNotificationSender> logger,
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
            _logger.LogInformation("Sending SMS to {Recipient}", recipient);

            var mockSend = _configuration["Sms:MockSend"];
            if (string.IsNullOrEmpty(mockSend) || mockSend == "true")
            {
                _logger.LogInformation(
                    "MOCK SMS SENT:\nTo: {To}\nMessage: {Message}",
                    recipient, body);

                var externalId = $"mock-sms-{Guid.NewGuid()}";
                await Task.CompletedTask;
                return new SendNotificationResult(true, externalId);
            }

            // Production Twilio integration would go here
            _logger.LogInformation("SMS sent successfully to {Recipient}", recipient);
            return new SendNotificationResult(true, $"sms-{DateTime.UtcNow.Ticks}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            return new SendNotificationResult(false, null, ex.Message);
        }
    }
}
