using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Inbox_Consumer_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "order_infra");

            migrationBuilder.CreateTable(
                name: "DeadLetterMessages",
                schema: "order_infra",
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
                    table.PrimaryKey("PK_DeadLetterMessages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "order_infra",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    consumer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_InboxMessages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "order_infra",
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
                    table.PrimaryKey("PK_OutboxMessages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SagaStates",
                schema: "order_infra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    StateJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_dlq_correlation_id",
                schema: "order_infra",
                table: "DeadLetterMessages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_event_type",
                schema: "order_infra",
                table: "DeadLetterMessages",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_moved_at",
                schema: "order_infra",
                table: "DeadLetterMessages",
                column: "moved_to_dlq_at");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_reprocessed",
                schema: "order_infra",
                table: "DeadLetterMessages",
                column: "reprocessed");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_name",
                schema: "order_infra",
                table: "DeadLetterMessages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_status_date",
                schema: "order_infra",
                table: "DeadLetterMessages",
                columns: new[] { "service_name", "reprocessed", "moved_to_dlq_at" });

            migrationBuilder.CreateIndex(
                name: "idx_inbox_correlation_id",
                schema: "order_infra",
                table: "InboxMessages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_consumer_unique",
                schema: "order_infra",
                table: "InboxMessages",
                columns: new[] { "event_id", "consumer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_processed_at",
                schema: "order_infra",
                table: "InboxMessages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_received_at",
                schema: "order_infra",
                table: "InboxMessages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_unprocessed",
                schema: "order_infra",
                table: "InboxMessages",
                columns: new[] { "processed_at", "processing_attempts" });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_correlation_id",
                schema: "order_infra",
                table: "OutboxMessages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_occurred_at",
                schema: "order_infra",
                table: "OutboxMessages",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_processed_at",
                schema: "order_infra",
                table: "OutboxMessages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                schema: "order_infra",
                table: "OutboxMessages",
                columns: new[] { "processed_at", "retry_count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadLetterMessages",
                schema: "order_infra");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "order_infra");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "order_infra");

            migrationBuilder.DropTable(
                name: "SagaStates",
                schema: "order_infra");
        }
    }
}
