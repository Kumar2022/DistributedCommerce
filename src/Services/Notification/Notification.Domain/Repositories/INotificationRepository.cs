using BuildingBlocks.Domain;
using Notification.Domain.ValueObjects;
using NotificationAggregate = Notification.Domain.Aggregates.Notification;
using NotificationTemplateAggregate = Notification.Domain.Aggregates.NotificationTemplate;

namespace Notification.Domain.Repositories;

public interface INotificationRepository
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<NotificationAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationAggregate>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationAggregate>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationAggregate>> GetScheduledNotificationsAsync(DateTime upTo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationAggregate>> GetFailedNotificationsForRetryAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationAggregate notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationAggregate notification, CancellationToken cancellationToken = default);
}

public interface INotificationTemplateRepository
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<NotificationTemplateAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationTemplateAggregate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplateAggregate>> GetByChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplateAggregate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplateAggregate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationTemplateAggregate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationTemplateAggregate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
