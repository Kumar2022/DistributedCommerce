using Notification.Application.DTOs;
using Notification.Application.IntegrationEvents;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.Application.Commands;

// ========== Send Notification Command ==========
public record SendNotificationCommand(
    Guid UserId,
    string Email,
    string Name,
    string? PhoneNumber,
    NotificationChannel Channel,
    string Subject,
    string Body,
    NotificationPriority Priority = NotificationPriority.Normal,
    Guid? TemplateId = null,
    Dictionary<string, string>? Variables = null,
    DateTime? ScheduledFor = null
) : ICommand<Guid>;

public class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand, Guid>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationRepository repository,
        INotificationTemplateRepository templateRepository,
        IEventBus eventBus,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _repository = repository;
        _templateRepository = templateRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create recipient
            var recipient = Recipient.Create(
                request.UserId,
                request.Email,
                request.Name,
                request.PhoneNumber
            );

            // Create content (or render from template)
            NotificationContent content;
            if (request.TemplateId.HasValue)
            {
                var template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken);
                if (template == null)
                    return Result.Failure<Guid>(Error.NotFound("Template", $"Template {request.TemplateId} not found"));

                content = template.RenderContent(request.Variables);
            }
            else
            {
                content = NotificationContent.Create(request.Subject, request.Body, request.Variables);
            }

            // Create notification
            var notification = NotificationAggregate.Create(
                recipient,
                request.Channel,
                content,
                request.Priority,
                request.TemplateId,
                request.ScheduledFor
            );

            await _repository.AddAsync(notification, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain events as integration events
            foreach (var domainEvent in notification.DomainEvents)
            {
                if (domainEvent is NotificationCreatedEvent created)
                {
                    await _eventBus.PublishAsync(new NotificationCreatedIntegrationEvent(
                        created.NotificationId,
                        created.UserId,
                        created.Channel.ToString(),
                        created.Subject,
                        created.OccurredAt
                    ), cancellationToken);
                }
            }

            _logger.LogInformation("Notification created: {NotificationId} for user {UserId}", 
                notification.Id, request.UserId);

            return Result.Success(notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", request.UserId);
            return Result.Failure<Guid>(Error.Unexpected("Failed to send notification"));
        }
    }
}

// ========== Mark As Sent Command ==========
public record MarkNotificationAsSentCommand(Guid NotificationId, string? ExternalId = null) : ICommand;

public class MarkNotificationAsSentCommandHandler : ICommandHandler<MarkNotificationAsSentCommand>
{
    private readonly INotificationRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MarkNotificationAsSentCommandHandler> _logger;

    public MarkNotificationAsSentCommandHandler(
        INotificationRepository repository,
        IEventBus eventBus,
        ILogger<MarkNotificationAsSentCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkNotificationAsSentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
                return Result.Failure(Error.NotFound("Notification", "Notification not found"));

            notification.MarkAsSent(request.ExternalId);
            await _repository.UpdateAsync(notification, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration event
            foreach (var domainEvent in notification.DomainEvents)
            {
                if (domainEvent is NotificationSentEvent sent)
                {
                    await _eventBus.PublishAsync(new NotificationSentIntegrationEvent(
                        sent.NotificationId,
                        sent.UserId,
                        sent.Channel.ToString(),
                        sent.SentAt,
                        sent.ExternalId
                    ), cancellationToken);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as sent: {NotificationId}", request.NotificationId);
            return Result.Failure(Error.Unexpected("Failed to mark notification as sent"));
        }
    }
}

// ========== Mark As Delivered Command ==========
public record MarkNotificationAsDeliveredCommand(Guid NotificationId, string? ExternalId = null) : ICommand;

public class MarkNotificationAsDeliveredCommandHandler : ICommandHandler<MarkNotificationAsDeliveredCommand>
{
    private readonly INotificationRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<MarkNotificationAsDeliveredCommandHandler> _logger;

    public MarkNotificationAsDeliveredCommandHandler(
        INotificationRepository repository,
        IEventBus eventBus,
        ILogger<MarkNotificationAsDeliveredCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(MarkNotificationAsDeliveredCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
                return Result.Failure(Error.NotFound("Notification", "Notification not found"));

            notification.MarkAsDelivered(request.ExternalId);
            await _repository.UpdateAsync(notification, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration event
            foreach (var domainEvent in notification.DomainEvents)
            {
                if (domainEvent is NotificationDeliveredEvent delivered)
                {
                    await _eventBus.PublishAsync(new NotificationDeliveredIntegrationEvent(
                        delivered.NotificationId,
                        delivered.UserId,
                        delivered.Channel.ToString(),
                        delivered.DeliveredAt,
                        delivered.ExternalId
                    ), cancellationToken);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as delivered: {NotificationId}", request.NotificationId);
            return Result.Failure(Error.Unexpected("Failed to mark notification as delivered"));
        }
    }
}
