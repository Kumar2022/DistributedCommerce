using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Aggregates;
using Shipping.Domain.ValueObjects;

namespace Shipping.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Shipment aggregate
/// </summary>
public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments", "shipping");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.OrderId)
            .IsRequired();

        builder.Property(s => s.TrackingNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.TrackingNumber)
            .IsUnique();

        builder.HasIndex(s => s.OrderId);

        builder.Property(s => s.Carrier)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.CarrierTrackingUrl)
            .HasMaxLength(500);

        // Configure ShippingAddress value object as owned entity
        builder.OwnsOne(s => s.ShippingAddress, address =>
        {
            address.Property(a => a.RecipientName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("RecipientName");

            address.Property(a => a.AddressLine1)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("AddressLine1");

            address.Property(a => a.AddressLine2)
                .HasMaxLength(500)
                .HasColumnName("AddressLine2");

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("City");

            address.Property(a => a.StateOrProvince)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("StateOrProvince");

            address.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("PostalCode");

            address.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Country");

            address.Property(a => a.Phone)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Phone");

            address.Property(a => a.Email)
                .HasMaxLength(200)
                .HasColumnName("Email");
        });

        // Configure Package value object as owned entity
        builder.OwnsOne(s => s.Package, package =>
        {
            package.Property(p => p.Weight)
                .HasPrecision(10, 2)
                .HasColumnName("PackageWeight");

            package.Property(p => p.Length)
                .HasPrecision(10, 2)
                .HasColumnName("PackageLength");

            package.Property(p => p.Width)
                .HasPrecision(10, 2)
                .HasColumnName("PackageWidth");

            package.Property(p => p.Height)
                .HasPrecision(10, 2)
                .HasColumnName("PackageHeight");

            package.Property(p => p.DimensionUnit)
                .HasMaxLength(10)
                .HasColumnName("PackageDimensionUnit");

            package.Property(p => p.WeightUnit)
                .HasMaxLength(10)
                .HasColumnName("PackageWeightUnit");
        });

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Status);

        builder.Property(s => s.DeliverySpeed)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.PickupTime);

        builder.Property(s => s.EstimatedDelivery);

        builder.Property(s => s.ActualDelivery);

        builder.Property(s => s.RecipientName)
            .HasMaxLength(200);

        builder.Property(s => s.SignatureUrl)
            .HasMaxLength(500);

        builder.Property(s => s.DeliveryAttempts)
            .HasDefaultValue(0);

        builder.Property(s => s.LastDeliveryFailureReason)
            .HasMaxLength(500);

        builder.Property(s => s.ShippingCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        // Configure TrackingHistory as owned collection
        builder.OwnsMany(s => s.TrackingHistory, tracking =>
        {
            tracking.ToTable("ShipmentTrackingHistory", "shipping");

            tracking.WithOwner()
                .HasForeignKey("ShipmentId");

            tracking.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnName("Id");

            tracking.HasKey("Id");

            tracking.Property(t => t.Location)
                .IsRequired()
                .HasMaxLength(300);

            tracking.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(100);

            tracking.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(1000);

            tracking.Property(t => t.Timestamp)
                .IsRequired();

            tracking.Property(t => t.Coordinates)
                .HasMaxLength(100);

            tracking.HasIndex("ShipmentId");
        });

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Ignore domain events (handled by MediatR)
        builder.Ignore(s => s.DomainEvents);
    }
}
