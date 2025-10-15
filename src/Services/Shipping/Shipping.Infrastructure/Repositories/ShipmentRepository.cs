using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Shipping.Domain.Aggregates;
using Shipping.Domain.Repositories;
using Shipping.Infrastructure.Persistence;

namespace Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Shipment aggregate
/// Uses Entity Framework Core for data access
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly ShippingDbContext _context;

    public ShipmentRepository(ShippingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public BuildingBlocks.Domain.IUnitOfWork UnitOfWork => (BuildingBlocks.Domain.IUnitOfWork)_context;

    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return null;

        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);
    }

    public async Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetByOrderIdsAsync(
        IEnumerable<Guid> orderIds,
        CancellationToken cancellationToken = default)
    {
        var orderIdsList = orderIds.ToList();
        
        return await _context.Shipments
            .Where(s => orderIdsList.Contains(s.OrderId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetPendingShipmentsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Where(s => s.Status == ShipmentStatus.Pending || s.Status == ShipmentStatus.PickupScheduled)
            .OrderBy(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetDelayedShipmentsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Shipments
            .Where(s => s.EstimatedDelivery.HasValue 
                     && s.EstimatedDelivery.Value < now 
                     && s.Status != ShipmentStatus.Delivered
                     && s.Status != ShipmentStatus.Cancelled)
            .OrderBy(s => s.EstimatedDelivery)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await _context.Shipments.AddAsync(shipment, cancellationToken);
    }

    public Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Shipments.Update(shipment);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Shipments.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> TrackingNumberExistsAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return false;

        return await _context.Shipments
            .AnyAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);
    }
}
