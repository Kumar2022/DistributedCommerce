using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Commands;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Payment repository with transactional outbox pattern (handled by BaseDbContext)
/// </summary>
public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(
        PaymentDbContext context,
        ILogger<PaymentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Domain.Aggregates.PaymentAggregate.Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.PaymentAggregate.Payment?> GetByExternalIdAsync(
        string externalPaymentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId, cancellationToken);
    }

    public async Task<List<Domain.Aggregates.PaymentAggregate.Payment>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.OrderId.Value == orderId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Domain.Aggregates.PaymentAggregate.Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    public Task UpdateAsync(
        Domain.Aggregates.PaymentAggregate.Payment payment,
        CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // BaseDbContext automatically handles:
        // 1. Extracting domain events
        // 2. Converting to outbox messages
        // 3. Saving in same transaction
        // 4. Clearing domain events
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Saved payment changes ({Count} entities modified)",
            result);
    }
}
