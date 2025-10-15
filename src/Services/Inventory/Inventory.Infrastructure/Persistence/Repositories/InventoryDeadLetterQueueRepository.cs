using BuildingBlocks.Infrastructure.DeadLetterQueue;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public sealed class InventoryDeadLetterQueueRepository : IDeadLetterQueueRepository
{
    private readonly InventoryDbContext _dbContext;

    public InventoryDeadLetterQueueRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        await _dbContext.DeadLetterMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DeadLetterMessage>> GetAsync(string? serviceName = null, DateTime? fromDate = null, DateTime? toDate = null, bool includeReprocessed = false, int pageSize = 100, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DeadLetterMessages.AsQueryable();
        if (!string.IsNullOrEmpty(serviceName)) query = query.Where(m => m.ServiceName == serviceName);
        if (fromDate.HasValue) query = query.Where(m => m.MovedToDlqAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.MovedToDlqAt <= toDate.Value);
        if (!includeReprocessed) query = query.Where(m => !m.Reprocessed);
        return await query.OrderByDescending(m => m.MovedToDlqAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeadLetterMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task MarkAsReprocessedAsync(Guid id, string? notes = null, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.DeadLetterMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message != null)
        {
            message.Reprocessed = true;
            if (!string.IsNullOrEmpty(notes))
                message.OperatorNotes = string.IsNullOrEmpty(message.OperatorNotes) ? notes : $"{message.OperatorNotes}\n{notes}";
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddNotesAsync(Guid id, string notes, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.DeadLetterMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message != null)
        {
            message.OperatorNotes = string.IsNullOrEmpty(message.OperatorNotes) ? notes : $"{message.OperatorNotes}\n{notes}";
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<DlqStatistics> GetStatisticsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DeadLetterMessages.AsQueryable();
        if (fromDate.HasValue) query = query.Where(m => m.MovedToDlqAt >= fromDate.Value);
        var totalMessages = await query.CountAsync(cancellationToken);
        var pendingMessages = await query.CountAsync(m => !m.Reprocessed, cancellationToken);
        var reprocessedMessages = await query.CountAsync(m => m.Reprocessed, cancellationToken);
        var messagesByService = await query.GroupBy(m => m.ServiceName).Select(g => new { Service = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Service, x => x.Count, cancellationToken);
        var messagesByEventType = await query.GroupBy(m => m.EventType).Select(g => new { EventType = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);
        var oldestMessage = await query.OrderBy(m => m.MovedToDlqAt).FirstOrDefaultAsync(cancellationToken);
        var newestMessage = await query.OrderByDescending(m => m.MovedToDlqAt).FirstOrDefaultAsync(cancellationToken);
        return new DlqStatistics
        {
            TotalMessages = totalMessages,
            PendingMessages = pendingMessages,
            ReprocessedMessages = reprocessedMessages,
            MessagesByService = messagesByService,
            MessagesByEventType = messagesByEventType,
            OldestMessageDate = oldestMessage?.MovedToDlqAt,
            NewestMessageDate = newestMessage?.MovedToDlqAt
        };
    }

    public async Task CleanupReprocessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;
        var oldMessages = await _dbContext.DeadLetterMessages.Where(m => m.Reprocessed && m.MovedToDlqAt < cutoffDate).ToListAsync(cancellationToken);
        _dbContext.DeadLetterMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
