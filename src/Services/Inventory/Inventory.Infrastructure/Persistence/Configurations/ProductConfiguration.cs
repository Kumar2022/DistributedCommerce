using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductId)
            .IsRequired();

        builder.HasIndex(p => p.ProductId)
            .IsUnique();

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.ReservedQuantity)
            .IsRequired();

        builder.Property(p => p.ReorderLevel)
            .IsRequired();

        builder.Property(p => p.ReorderQuantity)
            .IsRequired();

        builder.Property(p => p.LastRestockDate)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Optimistic concurrency
        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Navigation - owned collection
        builder.HasMany(p => p.Reservations as IEnumerable<StockReservation>)
            .WithOne()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events collection
        builder.Ignore(p => p.DomainEvents);
    }
}
