#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class UpdatedDraftStep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Captain1Id",
                table: "Drafts");

            migrationBuilder.DropColumn(
                name: "Captain2Id",
                table: "Drafts");

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "DraftStep",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<ulong>(
                name: "Captain1DiscordId",
                table: "Drafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "Captain2DiscordId",
                table: "Drafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Index",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "Captain1DiscordId",
                table: "Drafts");

            migrationBuilder.DropColumn(
                name: "Captain2DiscordId",
                table: "Drafts");

            migrationBuilder.AddColumn<string>(
                name: "Captain1Id",
                table: "Drafts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Captain2Id",
                table: "Drafts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
