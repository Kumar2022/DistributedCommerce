namespace Payment.Application.DTOs;

/// <summary>
/// Payment data transfer object
/// </summary>
public sealed record PaymentDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ExternalPaymentId { get; init; }
    public string? FailureReason { get; init; }
    public string? ErrorCode { get; init; }
    public decimal RefundedAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public DateTime? RefundedAt { get; init; }
}
