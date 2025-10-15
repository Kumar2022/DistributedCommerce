using Microsoft.EntityFrameworkCore;
using Notification.Domain.Repositories;
using Notification.Infrastructure.Persistence;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;

namespace Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<NotificationAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationAggregate>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Recipient.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationAggregate>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == status)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationAggregate>> GetScheduledNotificationsAsync(DateTime upTo, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.ScheduledFor != null && n.ScheduledFor <= upTo && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.ScheduledFor)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationAggregate>> GetFailedNotificationsForRetryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < 3)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Recipient.UserId == userId && n.Status != NotificationStatus.Delivered)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationAggregate notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public Task UpdateAsync(NotificationAggregate notification, CancellationToken cancellationToken = default)
    {
        _context.Entry(notification).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
