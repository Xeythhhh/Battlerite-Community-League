#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedContenderToStandard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipUpdatedBy",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ContenderQueue",
                table: "Users",
                newName: "StrictMatchmaking");

            migrationBuilder.RenameColumn(
                name: "Contender",
                table: "Users",
                newName: "Pro");

            migrationBuilder.RenameColumn(
                name: "GamesWon_Contender",
                table: "StatsSnapshots",
                newName: "GamesWon_Standard");

            migrationBuilder.RenameColumn(
                name: "GamesPlayed_Contender",
                table: "StatsSnapshots",
                newName: "GamesPlayed_Standard");

            migrationBuilder.AddColumn<int>(
                name: "PlacementGamesRemaining",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlacementGamesRemaining",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "StrictMatchmaking",
                table: "Users",
                newName: "ContenderQueue");

            migrationBuilder.RenameColumn(
                name: "Pro",
                table: "Users",
                newName: "Contender");

            migrationBuilder.RenameColumn(
                name: "GamesWon_Standard",
                table: "StatsSnapshots",
                newName: "GamesWon_Contender");

            migrationBuilder.RenameColumn(
                name: "GamesPlayed_Standard",
                table: "StatsSnapshots",
                newName: "GamesPlayed_Contender");

            migrationBuilder.AddColumn<string>(
                name: "MembershipUpdatedBy",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }
    }
}
