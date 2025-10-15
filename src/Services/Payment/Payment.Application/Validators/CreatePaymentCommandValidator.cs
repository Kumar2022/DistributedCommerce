using FluentValidation;
using Payment.Application.Commands;

namespace Payment.Application.Validators;

/// <summary>
/// Validator for CreatePaymentCommand
/// </summary>
public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters (ISO 4217)")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .Must(BeValidPaymentMethod)
            .WithMessage("Invalid payment method. Valid values: CreditCard, DebitCard, PayPal, BankTransfer, Crypto, Cash");
    }

    private bool BeValidPaymentMethod(string paymentMethod)
    {
        return Enum.TryParse<PaymentMethod>(paymentMethod, true, out _);
    }
}
