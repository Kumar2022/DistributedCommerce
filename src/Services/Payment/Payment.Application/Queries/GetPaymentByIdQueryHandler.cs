using Payment.Application.Commands;
using Payment.Application.DTOs;

namespace Payment.Application.Queries;

/// <summary>
/// Handler for getting a payment by ID
/// </summary>
public sealed class GetPaymentByIdQueryHandler : IQueryHandler<GetPaymentByIdQuery, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentDto>> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result.Failure<PaymentDto>(Error.NotFound(
                "Payment", request.PaymentId));
        }

        var dto = new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId.Value,
            Amount = payment.Amount.Amount,
            Currency = payment.Amount.Currency,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            ExternalPaymentId = payment.ExternalPaymentId,
            FailureReason = payment.FailureReason,
            ErrorCode = payment.ErrorCode,
            RefundedAmount = payment.RefundedAmount,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt,
            FailedAt = payment.FailedAt,
            RefundedAt = payment.RefundedAt
        };

        return Result.Success(dto);
    }
}
