using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MensajeService.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageHistoryCursorIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_created_at",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_created_at",
                table: "messages",
                columns: new[] { "conversation_id", "created_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_created_at",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_created_at",
                table: "messages",
                columns: new[] { "conversation_id", "created_at" });
        }
    }
}
