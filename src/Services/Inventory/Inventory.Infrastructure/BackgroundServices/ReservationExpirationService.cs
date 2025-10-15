using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to release expired stock reservations
/// Runs every 60 seconds to check for and release expired reservations
/// </summary>
public class ReservationExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpirationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public ReservationExpirationService(
        IServiceProvider serviceProvider,
        ILogger<ReservationExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired reservations");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Reservation Expiration Service stopped");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // Find products with active reservations that may have expired
        var productsWithExpiredReservations = await context.Products
            .Include(p => p.Reservations)
            .Where(p => p.Reservations.Any(r => 
                r.Status == ReservationStatus.Active && 
                r.ExpiresAt < DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        if (!productsWithExpiredReservations.Any())
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation(
            "Found {Count} products with expired reservations",
            productsWithExpiredReservations.Count);

        var totalReleased = 0;

        foreach (var product in productsWithExpiredReservations)
        {
            var result = product.ReleaseExpiredReservations();
            if (result.IsSuccess)
            {
                var expiredCount = product.DomainEvents
                    .OfType<Inventory.Domain.Events.ReservationExpiredEvent>()
                    .Count();
                
                totalReleased += expiredCount;
                context.Products.Update(product);
            }
        }

        if (totalReleased > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Released {Count} expired reservations",
                totalReleased);
        }
    }
}
