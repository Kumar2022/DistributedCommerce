namespace Notification.Application.DTOs;

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? ExternalId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public int RetryCount { get; init; }
    public string? ErrorMessage { get; init; }
}

public record NotificationTemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string SubjectTemplate { get; init; } = string.Empty;
    public string BodyTemplate { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? Category { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record SendNotificationRequest
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal";
    public Guid? TemplateId { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
}

public record CreateTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string SubjectTemplate { get; init; } = string.Empty;
    public string BodyTemplate { get; init; } = string.Empty;
    public string? Category { get; init; }
    public Dictionary<string, string>? DefaultVariables { get; init; }
}
