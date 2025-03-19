#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddedStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Useless",
                table: "Champions",
                newName: "PickRate");

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed",
                table: "Maps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BanRate",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesBanned",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesPicked",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GamesWon",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "BanRate",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesBanned",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesPicked",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesWon",
                table: "Champions");

            migrationBuilder.RenameColumn(
                name: "PickRate",
                table: "Champions",
                newName: "Useless");
        }
    }
}
