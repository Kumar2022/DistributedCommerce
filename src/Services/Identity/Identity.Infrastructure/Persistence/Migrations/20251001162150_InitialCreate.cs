using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "identity",
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
                schema: "identity",
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
                schema: "identity",
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
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_dlq_correlation_id",
                schema: "identity",
                table: "dead_letter_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_event_type",
                schema: "identity",
                table: "dead_letter_messages",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_moved_at",
                schema: "identity",
                table: "dead_letter_messages",
                column: "moved_to_dlq_at");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_reprocessed",
                schema: "identity",
                table: "dead_letter_messages",
                column: "reprocessed");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_name",
                schema: "identity",
                table: "dead_letter_messages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_status_date",
                schema: "identity",
                table: "dead_letter_messages",
                columns: new[] { "service_name", "reprocessed", "moved_to_dlq_at" });

            migrationBuilder.CreateIndex(
                name: "idx_inbox_correlation_id",
                schema: "identity",
                table: "inbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_id_unique",
                schema: "identity",
                table: "inbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_processed_at",
                schema: "identity",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_received_at",
                schema: "identity",
                table: "inbox_messages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_unprocessed",
                schema: "identity",
                table: "inbox_messages",
                columns: new[] { "processed_at", "processing_attempts" });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_correlation_id",
                schema: "identity",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_occurred_at",
                schema: "identity",
                table: "outbox_messages",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_processed_at",
                schema: "identity",
                table: "outbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                schema: "identity",
                table: "outbox_messages",
                columns: new[] { "processed_at", "retry_count" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "identity",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");
        }
    }
}
