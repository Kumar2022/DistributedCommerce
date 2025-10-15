using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "notification",
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
                schema: "notification",
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
                name: "Notifications",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Recipient_DeviceToken = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Recipient_Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Content_Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BodyTemplate = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultVariables = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "notification",
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

            migrationBuilder.CreateIndex(
                name: "idx_dlq_correlation_id",
                schema: "notification",
                table: "dead_letter_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_event_type",
                schema: "notification",
                table: "dead_letter_messages",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_moved_at",
                schema: "notification",
                table: "dead_letter_messages",
                column: "moved_to_dlq_at");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_reprocessed",
                schema: "notification",
                table: "dead_letter_messages",
                column: "reprocessed");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_name",
                schema: "notification",
                table: "dead_letter_messages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "idx_dlq_service_status_date",
                schema: "notification",
                table: "dead_letter_messages",
                columns: new[] { "service_name", "reprocessed", "moved_to_dlq_at" });

            migrationBuilder.CreateIndex(
                name: "idx_inbox_correlation_id",
                schema: "notification",
                table: "inbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_id_unique",
                schema: "notification",
                table: "inbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_processed_at",
                schema: "notification",
                table: "inbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_received_at",
                schema: "notification",
                table: "inbox_messages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_unprocessed",
                schema: "notification",
                table: "inbox_messages",
                columns: new[] { "processed_at", "processing_attempts" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel",
                schema: "notification",
                table: "Notifications",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                schema: "notification",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                schema: "notification",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_ScheduledFor",
                schema: "notification",
                table: "Notifications",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "notification",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Category",
                schema: "notification",
                table: "NotificationTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Channel",
                schema: "notification",
                table: "NotificationTemplates",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsActive",
                schema: "notification",
                table: "NotificationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Name",
                schema: "notification",
                table: "NotificationTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_outbox_correlation_id",
                schema: "notification",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_occurred_at",
                schema: "notification",
                table: "outbox_messages",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_processed_at",
                schema: "notification",
                table: "outbox_messages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_unprocessed",
                schema: "notification",
                table: "outbox_messages",
                columns: new[] { "processed_at", "retry_count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "notification");
        }
    }
}
