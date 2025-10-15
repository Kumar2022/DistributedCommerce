namespace Notification.Infrastructure.Services;

public interface INotificationSenderFactory
{
    INotificationSender GetSender(NotificationChannel channel);
}

public class NotificationSenderFactory : INotificationSenderFactory
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ILogger<NotificationSenderFactory> _logger;

    public NotificationSenderFactory(
        IEnumerable<INotificationSender> senders,
        ILogger<NotificationSenderFactory> logger)
    {
        _senders = senders;
        _logger = logger;
    }

    public INotificationSender GetSender(NotificationChannel channel)
    {
        var sender = _senders.FirstOrDefault(s => s.Channel == channel);
        
        if (sender == null)
        {
            _logger.LogError("No sender found for channel {Channel}", channel);
            throw new InvalidOperationException($"No sender configured for channel: {channel}");
        }

        return sender;
    }
}
