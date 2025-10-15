using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "customer_metrics",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    total_orders = table.Column<int>(type: "integer", nullable: false),
                    lifetime_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    average_order_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    first_order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    days_since_last_order = table.Column<int>(type: "integer", nullable: false),
                    customer_segment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    original_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    moved_to_dlq_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    total_attempts = table.Column<int>(type: "integer", nullable: false),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reprocessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    reprocessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    operator_notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letter_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    processing_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_metrics",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_orders = table.Column<int>(type: "integer", nullable: false),
                    completed_orders = table.Column<int>(type: "integer", nullable: false),
                    cancelled_orders = table.Column<int>(type: "integer", nullable: false),
                    pending_orders = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    average_order_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_metrics",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    metric_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    add_to_cart_count = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    conversion_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    inventory_level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "revenue_metrics",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    granularity = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    average_transaction_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revenue_metrics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_metrics_customer_id",
                schema: "analytics",
                table: "customer_metrics",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_metrics_lifetime_value",
                schema: "analytics",
                table: "customer_metrics",
                column: "lifetime_value");

            migrationBuilder.CreateIndex(
                name: "ix_customer_metrics_segment",
                schema: "analytics",
                table: "customer_metrics",
                column: "customer_segment");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_correlation_id",
                schema: "analytics",
                table: "dead_letter_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_event_type",
                schema: "analytics",
                table: "dead_letter_messages",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_moved_at",
                schema: "analytics",
                table: "dead_letter_messages",
                column: "moved_to_dlq_at");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_reprocessed",
                schema: "analytics",
                table: "dead_letter_messages",
                column: "reprocessed");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_name",
                schema: "analytics",
                table: "dead_letter_messages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_status_date",
                schema: "analytics",
                table: "dead_letter_messages",
                columns: new[] { "service_name", "reprocessed", "moved_to_dlq_at" });

            migrationBuilder.CreateIndex(
                name: "idx_inbox_correlation_id",
                schema: "analytics",
                table: "inbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_id_unique",
                schema: "analytics",
                table: "inbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_processed_at",
                schema: "analytics",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_received_at",
                schema: "analytics",
                table: "inbox_messages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_unprocessed",
                schema: "analytics",
                table: "inbox_messages",
                columns: new[] { "processed_at", "processing_attempts" });

            migrationBuilder.CreateIndex(
                name: "ix_order_metrics_metric_date",
                schema: "analytics",
                table: "order_metrics",
                column: "metric_date");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_correlation_id",
                schema: "analytics",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_occurred_at",
                schema: "analytics",
                table: "outbox_messages",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_processed_at",
                schema: "analytics",
                table: "outbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                schema: "analytics",
                table: "outbox_messages",
                columns: new[] { "processed_at", "retry_count" });

            migrationBuilder.CreateIndex(
                name: "ix_product_metrics_product_date",
                schema: "analytics",
                table: "product_metrics",
                columns: new[] { "product_id", "metric_date" });

            migrationBuilder.CreateIndex(
                name: "ix_product_metrics_purchase_count",
                schema: "analytics",
                table: "product_metrics",
                column: "purchase_count");

            migrationBuilder.CreateIndex(
                name: "ix_revenue_metrics_date_granularity",
                schema: "analytics",
                table: "revenue_metrics",
                columns: new[] { "metric_date", "granularity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_metrics",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "order_metrics",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "product_metrics",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "revenue_metrics",
                schema: "analytics");
        }
    }
}
