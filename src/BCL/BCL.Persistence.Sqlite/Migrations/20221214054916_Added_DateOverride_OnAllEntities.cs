#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_DateOverride_OnAllEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "StatsSnapshots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Stats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "RegionDraftTimes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "MigrationInfo",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Matches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Maps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "DraftStep",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Drafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "ChampionStats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOverride",
                table: "Champions",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "RegionDraftTimes");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "MigrationInfo");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Drafts");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "ChampionStats");

            migrationBuilder.DropColumn(
                name: "DateOverride",
                table: "Champions");
        }
    }
}
