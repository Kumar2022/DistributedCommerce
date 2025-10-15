using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "inventory",
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
                schema: "inventory",
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
                name: "outbox_messages",
                schema: "inventory",
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
                name: "products",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    ReorderQuantity = table.Column<int>(type: "integer", nullable: false),
                    LastRestockDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_reservations_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_dlq_correlation_id",
                schema: "inventory",
                table: "dead_letter_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_event_type",
                schema: "inventory",
                table: "dead_letter_messages",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_moved_at",
                schema: "inventory",
                table: "dead_letter_messages",
                column: "moved_to_dlq_at");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_reprocessed",
                schema: "inventory",
                table: "dead_letter_messages",
                column: "reprocessed");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_name",
                schema: "inventory",
                table: "dead_letter_messages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_status_date",
                schema: "inventory",
                table: "dead_letter_messages",
                columns: new[] { "service_name", "reprocessed", "moved_to_dlq_at" });

            migrationBuilder.CreateIndex(
                name: "idx_inbox_correlation_id",
                schema: "inventory",
                table: "inbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_id_unique",
                schema: "inventory",
                table: "inbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_processed_at",
                schema: "inventory",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_received_at",
                schema: "inventory",
                table: "inbox_messages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_unprocessed",
                schema: "inventory",
                table: "inbox_messages",
                columns: new[] { "processed_at", "processing_attempts" });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_correlation_id",
                schema: "inventory",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_occurred_at",
                schema: "inventory",
                table: "outbox_messages",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_processed_at",
                schema: "inventory",
                table: "outbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                schema: "inventory",
                table: "outbox_messages",
                columns: new[] { "processed_at", "retry_count" });

            migrationBuilder.CreateIndex(
                name: "IX_products_ProductId",
                schema: "inventory",
                table: "products",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_Sku",
                schema: "inventory",
                table: "products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ExpiresAt",
                schema: "inventory",
                table: "stock_reservations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_OrderId",
                schema: "inventory",
                table: "stock_reservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ProductId",
                schema: "inventory",
                table: "stock_reservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ProductId_OrderId_Status",
                schema: "inventory",
                table: "stock_reservations",
                columns: new[] { "ProductId", "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ReservationId",
                schema: "inventory",
                table: "stock_reservations",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_Status",
                schema: "inventory",
                table: "stock_reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_Status_ExpiresAt",
                schema: "inventory",
                table: "stock_reservations",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_reservations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "products",
                schema: "inventory");
        }
    }
}
