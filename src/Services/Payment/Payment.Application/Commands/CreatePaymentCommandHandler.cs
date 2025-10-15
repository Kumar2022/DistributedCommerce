using Payment.Application.DTOs;

namespace Payment.Application.Commands;

/// <summary>
/// Handler for creating a new payment
/// </summary>
public sealed class CreatePaymentCommandHandler : ICommandHandler<CreatePaymentCommand, Guid>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ILogger<CreatePaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating payment for order {OrderId} with amount {Amount} {Currency}",
            request.OrderId, request.Amount, request.Currency);

        // Parse payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return Result.Failure<Guid>(Error.Validation(
                "PaymentMethod",
                $"Invalid payment method: {request.PaymentMethod}"));
        }

        // Create value objects
        var orderId = OrderId.Create(request.OrderId);
        var moneyResult = Money.Create(request.Amount, request.Currency);

        if (moneyResult.IsFailure)
            return Result.Failure<Guid>(moneyResult.Error);

        // Create payment aggregate
        var paymentResult = Domain.Aggregates.PaymentAggregate.Payment.Create(
            orderId,
            moneyResult.Value,
            paymentMethod);

        if (paymentResult.IsFailure)
            return Result.Failure<Guid>(paymentResult.Error);

        var payment = paymentResult.Value;

        // Save to repository
        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} created successfully for order {OrderId}",
            payment.Id, request.OrderId);

        return Result.Success(payment.Id);
    }
}

/// <summary>
/// Repository interface for payments
/// </summary>
public interface IPaymentRepository
{
    Task<Domain.Aggregates.PaymentAggregate.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.PaymentAggregate.Payment?> GetByExternalIdAsync(string externalPaymentId, CancellationToken cancellationToken = default);
    Task<List<Domain.Aggregates.PaymentAggregate.Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Aggregates.PaymentAggregate.Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Aggregates.PaymentAggregate.Payment payment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
