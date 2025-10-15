using Notification.Application.DTOs;
using Notification.Application.IntegrationEvents;

namespace Notification.Application.Commands;

// ========== Create Template Command ==========
public record CreateTemplateCommand(
    string Name,
    string Description,
    NotificationChannel Channel,
    string SubjectTemplate,
    string BodyTemplate,
    string CreatedBy,
    string? Category = null,
    Dictionary<string, string>? DefaultVariables = null
) : ICommand<Guid>;

public class CreateTemplateCommandHandler : ICommandHandler<CreateTemplateCommand, Guid>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<CreateTemplateCommandHandler> _logger;

    public CreateTemplateCommandHandler(
        INotificationTemplateRepository repository,
        ILogger<CreateTemplateCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if template with same name already exists
            var existing = await _repository.GetByNameAsync(request.Name, cancellationToken);
            if (existing != null)
                return Result.Failure<Guid>(Error.Conflict($"Template with name '{request.Name}' already exists"));

            var template = NotificationTemplate.Create(
                request.Name,
                request.Description,
                request.Channel,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.CreatedBy,
                request.Category,
                request.DefaultVariables
            );

            await _repository.AddAsync(template, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template created: {TemplateId} - {TemplateName}", template.Id, template.Name);

            return Result.Success(template.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template: {TemplateName}", request.Name);
            return Result.Failure<Guid>(Error.Unexpected("Failed to create template"));
        }
    }
}

// ========== Update Template Command ==========
public record UpdateTemplateCommand(
    Guid TemplateId,
    string Name,
    string Description,
    string SubjectTemplate,
    string BodyTemplate,
    string UpdatedBy,
    string? Category = null,
    Dictionary<string, string>? DefaultVariables = null
) : ICommand;

public class UpdateTemplateCommandHandler : ICommandHandler<UpdateTemplateCommand>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<UpdateTemplateCommandHandler> _logger;

    public UpdateTemplateCommandHandler(
        INotificationTemplateRepository repository,
        ILogger<UpdateTemplateCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template == null)
                return Result.Failure(Error.NotFound("Template", "Template not found"));

            template.Update(
                request.Name,
                request.Description,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.UpdatedBy,
                request.Category,
                request.DefaultVariables
            );

            await _repository.UpdateAsync(template, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Template updated: {TemplateId}", request.TemplateId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template: {TemplateId}", request.TemplateId);
            return Result.Failure(Error.Unexpected("Failed to update template"));
        }
    }
}

// ========== Activate/Deactivate Template Commands ==========
public record ActivateTemplateCommand(Guid TemplateId) : ICommand;

public class ActivateTemplateCommandHandler : ICommandHandler<ActivateTemplateCommand>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<ActivateTemplateCommandHandler> _logger;

    public ActivateTemplateCommandHandler(
        INotificationTemplateRepository repository,
        ILogger<ActivateTemplateCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivateTemplateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template == null)
                return Result.Failure(Error.NotFound("Template", "Template not found"));

            template.Activate();
            await _repository.UpdateAsync(template, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating template: {TemplateId}", request.TemplateId);
            return Result.Failure(Error.Unexpected("Failed to activate template"));
        }
    }
}

public record DeactivateTemplateCommand(Guid TemplateId) : ICommand;

public class DeactivateTemplateCommandHandler : ICommandHandler<DeactivateTemplateCommand>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<DeactivateTemplateCommandHandler> _logger;

    public DeactivateTemplateCommandHandler(
        INotificationTemplateRepository repository,
        ILogger<DeactivateTemplateCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivateTemplateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template == null)
                return Result.Failure(Error.NotFound("Template", "Template not found"));

            template.Deactivate();
            await _repository.UpdateAsync(template, cancellationToken);
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating template: {TemplateId}", request.TemplateId);
            return Result.Failure(Error.Unexpected("Failed to deactivate template"));
        }
    }
}
