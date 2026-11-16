using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MensajeService.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "conversations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "conversations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "conversation_participants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.AddCheckConstraint(
                name: "ck_conversations_group_metadata",
                table: "conversations",
                sql: "(type = 'Direct' AND name IS NULL AND description IS NULL AND created_by IS NULL) OR (type = 'Group' AND name IS NOT NULL AND length(trim(name)) > 0 AND created_by IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_conversation_participants_valid_role",
                table: "conversation_participants",
                sql: "role IN ('Member', 'Admin')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_conversations_group_metadata",
                table: "conversations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_conversation_participants_valid_role",
                table: "conversation_participants");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "description",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "name",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "role",
                table: "conversation_participants");
        }
    }
}
