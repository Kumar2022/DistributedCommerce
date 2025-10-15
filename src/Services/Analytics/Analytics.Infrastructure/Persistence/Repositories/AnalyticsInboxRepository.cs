using System.Diagnostics;
using BuildingBlocks.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsInboxRepository(AnalyticsDbContext dbContext) : IInboxRepository
{
    private readonly AnalyticsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        return await _dbContext.InboxMessages.AnyAsync(m => m.EventId == eventId, cancellationToken);
    }

    public async Task<bool> AddAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var exists = await ExistsAsync(message.EventId, cancellationToken);
        if (exists) return false;
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        await _dbContext.InboxMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        var message = await _dbContext.InboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        var message = await _dbContext.InboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message != null)
        {
            message.ProcessingAttempts++;
            message.Error = error;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<InboxMessage>> GetUnprocessedAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        return await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt == null && m.ProcessingAttempts < 5)
            .OrderBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InboxMessage>> GetFailedMessagesAsync(int maxAttempts = 3, int batchSize = 100, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        return await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt == null && m.ProcessingAttempts >= maxAttempts)
            .OrderBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        Debug.Assert(_dbContext.InboxMessages != null, "_dbContext.InboxMessages != null");
        var oldMessages = await _dbContext.InboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
            .ToListAsync(cancellationToken);
        _dbContext.InboxMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
