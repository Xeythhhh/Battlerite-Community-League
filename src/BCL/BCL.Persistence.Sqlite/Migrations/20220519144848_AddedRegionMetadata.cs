#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddedRegionMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NaOnly",
                table: "Users",
                newName: "Server");

            migrationBuilder.RenameColumn(
                name: "EuOnly",
                table: "Users",
                newName: "NaePing");

            migrationBuilder.AddColumn<bool>(
                name: "Eu",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EuwPing",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Na",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileVersion",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Eu",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EuwPing",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Na",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileVersion",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Server",
                table: "Users",
                newName: "NaOnly");

            migrationBuilder.RenameColumn(
                name: "NaePing",
                table: "Users",
                newName: "EuOnly");
        }
    }
}
