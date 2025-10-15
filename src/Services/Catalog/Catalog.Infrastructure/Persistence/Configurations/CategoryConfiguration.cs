namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Category aggregate
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.DisplayOrder)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Add indexes
        builder.HasIndex(c => c.ParentCategoryId);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.DisplayOrder);

        // Ignore domain events (handled by BaseDbContext)
        builder.Ignore(c => c.DomainEvents);
    }
}
