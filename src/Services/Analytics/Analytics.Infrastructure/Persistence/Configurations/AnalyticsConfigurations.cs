namespace Analytics.Infrastructure.Persistence.Configurations;

public class OrderMetricsConfiguration : IEntityTypeConfiguration<OrderMetrics>
{
    public void Configure(EntityTypeBuilder<OrderMetrics> builder)
    {
        builder.ToTable("order_metrics");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.MetricDate)
            .HasColumnName("metric_date")
            .IsRequired();

        builder.Property(o => o.TotalOrders)
            .HasColumnName("total_orders")
            .IsRequired();

        builder.Property(o => o.CompletedOrders)
            .HasColumnName("completed_orders")
            .IsRequired();

        builder.Property(o => o.CancelledOrders)
            .HasColumnName("cancelled_orders")
            .IsRequired();

        builder.Property(o => o.PendingOrders)
            .HasColumnName("pending_orders")
            .IsRequired();

        builder.Property(o => o.TotalRevenue)
            .HasColumnName("total_revenue")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.AverageOrderValue)
            .HasColumnName("average_order_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(o => o.MetricDate)
            .HasDatabaseName("ix_order_metrics_metric_date");
    }
}

public class ProductMetricsConfiguration : IEntityTypeConfiguration<ProductMetrics>
{
    public void Configure(EntityTypeBuilder<ProductMetrics> builder)
    {
        builder.ToTable("product_metrics");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(p => p.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.MetricDate)
            .HasColumnName("metric_date")
            .IsRequired();

        builder.Property(p => p.ViewCount)
            .HasColumnName("view_count")
            .IsRequired();

        builder.Property(p => p.AddToCartCount)
            .HasColumnName("add_to_cart_count")
            .IsRequired();

        builder.Property(p => p.PurchaseCount)
            .HasColumnName("purchase_count")
            .IsRequired();

        builder.Property(p => p.TotalRevenue)
            .HasColumnName("total_revenue")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.ConversionRate)
            .HasColumnName("conversion_rate")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(p => p.InventoryLevel)
            .HasColumnName("inventory_level")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => new { p.ProductId, p.MetricDate })
            .HasDatabaseName("ix_product_metrics_product_date");

        builder.HasIndex(p => p.PurchaseCount)
            .HasDatabaseName("ix_product_metrics_purchase_count");
    }
}

public class CustomerMetricsConfiguration : IEntityTypeConfiguration<CustomerMetrics>
{
    public void Configure(EntityTypeBuilder<CustomerMetrics> builder)
    {
        builder.ToTable("customer_metrics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(c => c.CustomerEmail)
            .HasColumnName("customer_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.TotalOrders)
            .HasColumnName("total_orders")
            .IsRequired();

        builder.Property(c => c.LifetimeValue)
            .HasColumnName("lifetime_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.AverageOrderValue)
            .HasColumnName("average_order_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.FirstOrderDate)
            .HasColumnName("first_order_date")
            .IsRequired();

        builder.Property(c => c.LastOrderDate)
            .HasColumnName("last_order_date");

        builder.Property(c => c.DaysSinceLastOrder)
            .HasColumnName("days_since_last_order")
            .IsRequired();

        builder.Property(c => c.CustomerSegment)
            .HasColumnName("customer_segment")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(c => c.CustomerId)
            .IsUnique()
            .HasDatabaseName("ix_customer_metrics_customer_id");

        builder.HasIndex(c => c.CustomerSegment)
            .HasDatabaseName("ix_customer_metrics_segment");

        builder.HasIndex(c => c.LifetimeValue)
            .HasDatabaseName("ix_customer_metrics_lifetime_value");
    }
}

public class RevenueMetricsConfiguration : IEntityTypeConfiguration<RevenueMetrics>
{
    public void Configure(EntityTypeBuilder<RevenueMetrics> builder)
    {
        builder.ToTable("revenue_metrics");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.MetricDate)
            .HasColumnName("metric_date")
            .IsRequired();

        builder.Property(r => r.Granularity)
            .HasColumnName("granularity")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.TotalRevenue)
            .HasColumnName("total_revenue")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.RefundedAmount)
            .HasColumnName("refunded_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.NetRevenue)
            .HasColumnName("net_revenue")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.TransactionCount)
            .HasColumnName("transaction_count")
            .IsRequired();

        builder.Property(r => r.AverageTransactionValue)
            .HasColumnName("average_transaction_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(r => new { r.MetricDate, r.Granularity })
            .HasDatabaseName("ix_revenue_metrics_date_granularity");
    }
}
