using Payment.Application.Commands;
using Payment.Application.DTOs;

namespace Payment.Application.Queries;

/// <summary>
/// Handler for getting payments by order ID
/// </summary>
public sealed class GetPaymentsByOrderIdQueryHandler : IQueryHandler<GetPaymentsByOrderIdQuery, List<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsByOrderIdQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<List<PaymentDto>>> Handle(
        GetPaymentsByOrderIdQuery request,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);

        var dtos = payments.Select(payment => new PaymentDto
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
        }).ToList();

        return Result.Success(dtos);
    }
}
