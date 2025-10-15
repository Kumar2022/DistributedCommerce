using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Payment entity
/// </summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Domain.Aggregates.PaymentAggregate.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.PaymentAggregate.Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.Method)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.ExternalPaymentId)
            .HasMaxLength(255);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(1000);

        builder.Property(p => p.ErrorCode)
            .HasMaxLength(100);

        builder.Property(p => p.RefundedAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // OrderId value object
        builder.OwnsOne(p => p.OrderId, orderIdBuilder =>
        {
            orderIdBuilder.Property(o => o.Value)
                .HasColumnName("order_id")
                .IsRequired();
        });

        // Money value object
        builder.OwnsOne(p => p.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(p => p.ExternalPaymentId)
            .IsUnique()
            .HasFilter("external_payment_id IS NOT NULL");

        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.Status);

        // Ignore domain events (they're not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}
