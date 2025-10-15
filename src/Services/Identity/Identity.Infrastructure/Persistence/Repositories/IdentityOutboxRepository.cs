using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class IdentityOutboxRepository : IOutboxRepository
{
    private readonly IdentityDbContext _dbContext;

    public IdentityOutboxRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetFailedMessagesAsync(int maxRetries = 3, int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount >= maxRetries)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            message.Error = error;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var oldMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
            .ToListAsync(cancellationToken);
        _dbContext.OutboxMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
