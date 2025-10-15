using BuildingBlocks.Domain;
using Shipping.Domain.Aggregates;

namespace Shipping.Domain.Repositories;

/// <summary>
/// Repository interface for Shipment aggregate
/// </summary>
public interface IShipmentRepository
{
    IUnitOfWork UnitOfWork { get; }
    
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Shipment>> GetByOrderIdsAsync(IEnumerable<Guid> orderIds, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Shipment>> GetPendingShipmentsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Shipment>> GetDelayedShipmentsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> TrackingNumberExistsAsync(string trackingNumber, CancellationToken cancellationToken = default);
}
