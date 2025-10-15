using Notification.Application.DTOs;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.Application.Queries;

// ========== Get Notification By ID Query ==========
public record GetNotificationByIdQuery(Guid NotificationId) : IQuery<NotificationDto?>;

public class GetNotificationByIdQueryHandler : IQueryHandler<GetNotificationByIdQuery, NotificationDto?>
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<GetNotificationByIdQueryHandler> _logger;

    public GetNotificationByIdQueryHandler(
        INotificationRepository repository,
        ILogger<GetNotificationByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<NotificationDto?>> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
                return Result.Success<NotificationDto?>(null);

            var dto = MapToDto(notification);
            return Result.Success<NotificationDto?>(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification: {NotificationId}", request.NotificationId);
            return Result.Failure<NotificationDto?>(Error.Unexpected("Failed to retrieve notification"));
        }
    }

    private static NotificationDto MapToDto(NotificationAggregate notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.Recipient.UserId,
            UserEmail = notification.Recipient.Email,
            UserName = notification.Recipient.Name,
            Channel = notification.Channel.ToString(),
            Subject = notification.Content.Subject,
            Body = notification.Content.Body,
            Status = notification.Status.ToString(),
            Priority = notification.Priority.ToString(),
            ExternalId = notification.ExternalId,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            DeliveredAt = notification.DeliveredAt,
            RetryCount = notification.RetryCount,
            ErrorMessage = notification.ErrorMessage
        };
    }
}

// ========== Get Notifications By User ID Query ==========
public record GetNotificationsByUserIdQuery(Guid UserId) : IQuery<List<NotificationDto>>;

public class GetNotificationsByUserIdQueryHandler : IQueryHandler<GetNotificationsByUserIdQuery, List<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<GetNotificationsByUserIdQueryHandler> _logger;

    public GetNotificationsByUserIdQueryHandler(
        INotificationRepository repository,
        ILogger<GetNotificationsByUserIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetNotificationsByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
            var dtos = notifications.Select(MapToDto).ToList();
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user: {UserId}", request.UserId);
            return Result.Failure<List<NotificationDto>>(Error.Unexpected("Failed to retrieve notifications"));
        }
    }

    private static NotificationDto MapToDto(NotificationAggregate notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.Recipient.UserId,
            UserEmail = notification.Recipient.Email,
            UserName = notification.Recipient.Name,
            Channel = notification.Channel.ToString(),
            Subject = notification.Content.Subject,
            Body = notification.Content.Body,
            Status = notification.Status.ToString(),
            Priority = notification.Priority.ToString(),
            ExternalId = notification.ExternalId,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            DeliveredAt = notification.DeliveredAt,
            RetryCount = notification.RetryCount,
            ErrorMessage = notification.ErrorMessage
        };
    }
}

// ========== Get Template By ID Query ==========
public record GetTemplateByIdQuery(Guid TemplateId) : IQuery<NotificationTemplateDto?>;

public class GetTemplateByIdQueryHandler : IQueryHandler<GetTemplateByIdQuery, NotificationTemplateDto?>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<GetTemplateByIdQueryHandler> _logger;

    public GetTemplateByIdQueryHandler(
        INotificationTemplateRepository repository,
        ILogger<GetTemplateByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<NotificationTemplateDto?>> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template == null)
                return Result.Success<NotificationTemplateDto?>(null);

            var dto = MapToDto(template);
            return Result.Success<NotificationTemplateDto?>(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template: {TemplateId}", request.TemplateId);
            return Result.Failure<NotificationTemplateDto?>(Error.Unexpected("Failed to retrieve template"));
        }
    }

    private static NotificationTemplateDto MapToDto(NotificationTemplate template)
    {
        return new NotificationTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Channel = template.Channel.ToString(),
            SubjectTemplate = template.SubjectTemplate,
            BodyTemplate = template.BodyTemplate,
            IsActive = template.IsActive,
            Category = template.Category,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

// ========== Get Active Templates Query ==========
public record GetActiveTemplatesQuery : IQuery<List<NotificationTemplateDto>>;

public class GetActiveTemplatesQueryHandler : IQueryHandler<GetActiveTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<GetActiveTemplatesQueryHandler> _logger;

    public GetActiveTemplatesQueryHandler(
        INotificationTemplateRepository repository,
        ILogger<GetActiveTemplatesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<NotificationTemplateDto>>> Handle(GetActiveTemplatesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var templates = await _repository.GetActiveTemplatesAsync(cancellationToken);
            var dtos = templates.Select(MapToDto).ToList();
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active templates");
            return Result.Failure<List<NotificationTemplateDto>>(Error.Unexpected("Failed to retrieve templates"));
        }
    }

    private static NotificationTemplateDto MapToDto(NotificationTemplate template)
    {
        return new NotificationTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Channel = template.Channel.ToString(),
            SubjectTemplate = template.SubjectTemplate,
            BodyTemplate = template.BodyTemplate,
            IsActive = template.IsActive,
            Category = template.Category,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
