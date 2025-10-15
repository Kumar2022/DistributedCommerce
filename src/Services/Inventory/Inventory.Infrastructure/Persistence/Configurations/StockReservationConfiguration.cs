using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReservationId)
            .IsRequired();

        builder.HasIndex(r => r.ReservationId)
            .IsUnique();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.HasIndex(r => r.ProductId);

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.HasIndex(r => r.OrderId);

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.ReservedAt)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.HasIndex(r => r.ExpiresAt);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(r => r.Status);

        builder.Property(r => r.ConfirmedAt);

        builder.Property(r => r.ReleasedAt);

        // Composite index for queries
        builder.HasIndex(r => new { r.ProductId, r.OrderId, r.Status });
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });
    }
}
