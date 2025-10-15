using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Inbox_Consumer_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_inbox_event_id_unique",
                schema: "notification",
                table: "inbox_messages");

            migrationBuilder.AddColumn<string>(
                name: "consumer",
                schema: "notification",
                table: "inbox_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_consumer_unique",
                schema: "notification",
                table: "inbox_messages",
                columns: new[] { "event_id", "consumer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_inbox_event_consumer_unique",
                schema: "notification",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "consumer",
                schema: "notification",
                table: "inbox_messages");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_event_id_unique",
                schema: "notification",
                table: "inbox_messages",
                column: "event_id",
                unique: true);
        }
    }
}
