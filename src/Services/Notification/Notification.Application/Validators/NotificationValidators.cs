using FluentValidation;
using Notification.Application.Commands;

namespace Notification.Application.Validators;

public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MaximumLength(500).WithMessage("Subject must not exceed 500 characters");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required");

        RuleFor(x => x.Channel)
            .IsInEnum().WithMessage("Invalid notification channel");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority");
    }
}

public class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required");

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("Body template is required");

        RuleFor(x => x.Channel)
            .IsInEnum().WithMessage("Invalid notification channel");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("Created by is required");
    }
}

public class UpdateTemplateCommandValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Template ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.SubjectTemplate)
            .NotEmpty().WithMessage("Subject template is required");

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("Body template is required");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("Updated by is required");
    }
}
