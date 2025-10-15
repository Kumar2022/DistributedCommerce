namespace Notification.Domain.ValueObjects;

/// <summary>
/// Notification channel types
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    SMS = 2,
    Push = 3,
    InApp = 4
}

/// <summary>
/// Notification status
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Cancelled = 5
}

/// <summary>
/// Notification priority
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Represents a notification recipient
/// </summary>
public record Recipient
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? DeviceToken { get; init; }
    public string Name { get; init; }
    public Dictionary<string, string> Metadata { get; init; }

    private Recipient() 
    {
        Email = string.Empty;
        Name = string.Empty;
        Metadata = new Dictionary<string, string>();
    }

    public static Recipient Create(
        Guid userId,
        string email,
        string name,
        string? phoneNumber = null,
        string? deviceToken = null,
        Dictionary<string, string>? metadata = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new Recipient
        {
            UserId = userId,
            Email = email,
            Name = name,
            PhoneNumber = phoneNumber,
            DeviceToken = deviceToken,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }
}

/// <summary>
/// Notification content
/// </summary>
public record NotificationContent
{
    public string Subject { get; init; }
    public string Body { get; init; }
    public Dictionary<string, string> Variables { get; init; }
    public Dictionary<string, string> Metadata { get; init; }

    private NotificationContent()
    {
        Subject = string.Empty;
        Body = string.Empty;
        Variables = new Dictionary<string, string>();
        Metadata = new Dictionary<string, string>();
    }

    public static NotificationContent Create(
        string subject,
        string body,
        Dictionary<string, string>? variables = null,
        Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required", nameof(body));

        return new NotificationContent
        {
            Subject = subject,
            Body = body,
            Variables = variables ?? new Dictionary<string, string>(),
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }
}

/// <summary>
/// Delivery result information
/// </summary>
public record DeliveryResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public Dictionary<string, string> Metadata { get; init; }

    private DeliveryResult()
    {
        Metadata = new Dictionary<string, string>();
    }

    public static DeliveryResult Success(string? externalId = null, Dictionary<string, string>? metadata = null)
    {
        return new DeliveryResult
        {
            IsSuccess = true,
            ExternalId = externalId,
            DeliveredAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }

    public static DeliveryResult Failure(string errorMessage, Dictionary<string, string>? metadata = null)
    {
        return new DeliveryResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }
}
