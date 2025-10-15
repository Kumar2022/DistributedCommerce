using Microsoft.EntityFrameworkCore;
using Notification.Domain.Aggregates;
using Notification.Domain.Repositories;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.Repositories;

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public NotificationTemplateRepository(NotificationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> GetByChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Where(t => t.Channel == channel && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Where(t => t.Category == category && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.NotificationTemplates.AddAsync(template, cancellationToken);
    }

    public Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        _context.Entry(template).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(id, cancellationToken);
        if (template != null)
        {
            _context.NotificationTemplates.Remove(template);
        }
    }
}
