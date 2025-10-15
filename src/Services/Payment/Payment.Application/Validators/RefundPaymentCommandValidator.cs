using FluentValidation;
using Payment.Application.Commands;

namespace Payment.Application.Validators;

/// <summary>
/// Validator for RefundPaymentCommand
/// </summary>
public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID is required");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0)
            .WithMessage("Refund amount must be greater than zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Refund reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}
