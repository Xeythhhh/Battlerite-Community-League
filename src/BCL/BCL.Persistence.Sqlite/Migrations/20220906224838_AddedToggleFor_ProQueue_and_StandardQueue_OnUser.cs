#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddedToggleFor_ProQueue_and_StandardQueue_OnUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProQueue",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StandardQueue",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProQueue",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StandardQueue",
                table: "Users");
        }
    }
}
