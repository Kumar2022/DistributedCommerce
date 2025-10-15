using BuildingBlocks.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Payment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing inbox messages in Payment service.
/// Provides idempotent event consumption via EventId deduplication.
/// </summary>
public sealed class PaymentInboxRepository : IInboxRepository
{
    private readonly PaymentDbContext _dbContext;

    public PaymentInboxRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .AnyAsync(m => m.EventId == eventId, cancellationToken);
    }

    public async Task<bool> AddAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        // Check if message already exists
        var exists = await ExistsAsync(message.EventId, cancellationToken);
        if (exists)
            return false;

        await _dbContext.InboxMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(
        Guid messageId, 
        string error, 
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.InboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.ProcessingAttempts++;
            message.Error = error;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<InboxMessage>> GetUnprocessedAsync(
        int batchSize = 100, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt == null && m.ProcessingAttempts < 5)
            .OrderBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InboxMessage>> GetFailedMessagesAsync(
        int maxAttempts = 3,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt == null && m.ProcessingAttempts >= maxAttempts)
            .OrderBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupOldMessagesAsync(
        TimeSpan olderThan, 
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        
        var oldMessages = await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        _dbContext.InboxMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
