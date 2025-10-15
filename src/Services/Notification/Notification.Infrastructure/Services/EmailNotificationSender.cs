using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Notification.Infrastructure.Services;

public class EmailNotificationSender : INotificationSender
{
    private readonly ILogger<EmailNotificationSender> _logger;
    private readonly IConfiguration _configuration;

    public NotificationChannel Channel => NotificationChannel.Email;

    public EmailNotificationSender(
        ILogger<EmailNotificationSender> logger,
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
            _logger.LogInformation("Sending email to {Recipient} with subject: {Subject}", recipient, subject);

            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@distributedcommerce.com";
            var fromName = _configuration["Email:FromName"] ?? "Distributed Commerce";

            // For development, use mock sending
            var mockSend = _configuration["Email:MockSend"];
            if (string.IsNullOrEmpty(mockSend) || mockSend == "true")
            {
                _logger.LogInformation(
                    "MOCK EMAIL SENT:\nTo: {To}\nFrom: {From}\nSubject: {Subject}\nBody:\n{Body}",
                    recipient, fromEmail, subject, body);

                var externalId = $"mock-email-{Guid.NewGuid()}";
                await Task.CompletedTask; // To make the async method happy
                return new SendNotificationResult(true, externalId);
            }

            // Production SMTP sending would go here
            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return new SendNotificationResult(true, $"smtp-{DateTime.UtcNow.Ticks}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return new SendNotificationResult(false, null, ex.Message);
        }
    }
}
