#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedUserToSupportMultipleLeagues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StrictMatchmaking",
                table: "Users",
                newName: "PlacementGamesRemaining_Standard");

            migrationBuilder.AddColumn<bool>(
                name: "Declined",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Rating_Standard",
                table: "Users",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Rating_Standard",
                table: "StatsSnapshots",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HighestRating_Standard",
                table: "Stats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LowestRating_Standard",
                table: "Stats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Declined",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rating_Standard",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rating_Standard",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "HighestRating_Standard",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "LowestRating_Standard",
                table: "Stats");

            migrationBuilder.RenameColumn(
                name: "PlacementGamesRemaining_Standard",
                table: "Users",
                newName: "StrictMatchmaking");
        }
    }
}
