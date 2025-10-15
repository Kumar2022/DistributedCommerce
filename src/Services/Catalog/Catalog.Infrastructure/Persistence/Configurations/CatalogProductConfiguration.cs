namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for CatalogProduct aggregate
/// </summary>
public class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.AvailableQuantity)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.IsFeatured)
            .IsRequired();

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.SeoTitle)
            .HasMaxLength(200);

        builder.Property(p => p.SeoDescription)
            .HasMaxLength(500);

        builder.Property(p => p.Tags)
            .HasConversion(
                tags => string.Join(',', tags),
                tags => tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.PublishedAt);

        // Owned collections for Images
        builder.OwnsMany(p => p.Images, images =>
        {
            images.ToTable("ProductImages");
            images.WithOwner().HasForeignKey("ProductId");
            images.HasKey(i => i.Id);

            images.Property(i => i.Id)
                .IsRequired()
                .ValueGeneratedNever();

            images.Property(i => i.Url)
                .IsRequired()
                .HasMaxLength(500);

            images.Property(i => i.AltText)
                .HasMaxLength(200);

            images.Property(i => i.DisplayOrder)
                .IsRequired();

            images.Property(i => i.IsPrimary)
                .IsRequired();
        });

        // Owned collections for Attributes
        builder.OwnsMany(p => p.Attributes, attributes =>
        {
            attributes.ToTable("ProductAttributes");
            attributes.WithOwner().HasForeignKey("ProductId");
            attributes.HasKey(a => a.Id);

            attributes.Property(a => a.Id)
                .IsRequired()
                .ValueGeneratedNever();

            attributes.Property(a => a.Key)
                .IsRequired()
                .HasMaxLength(100);

            attributes.Property(a => a.Value)
                .IsRequired()
                .HasMaxLength(500);

            attributes.Property(a => a.DisplayName)
                .HasMaxLength(100);
        });

        // Add indexes for common queries
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Price);
        builder.HasIndex(p => p.CreatedAt);

        // Ignore domain events (handled by BaseDbContext)
        builder.Ignore(p => p.DomainEvents);
    }
}
