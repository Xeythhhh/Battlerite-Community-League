#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_Stats_OnChampion2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Picked_Tournament",
                table: "StatsSnapshots",
                newName: "MatchCount_Tournament");

            migrationBuilder.RenameColumn(
                name: "Picked_Standard",
                table: "StatsSnapshots",
                newName: "MatchCount_Standard");

            migrationBuilder.RenameColumn(
                name: "Picked_Pro",
                table: "StatsSnapshots",
                newName: "MatchCount_Pro");

            migrationBuilder.RenameColumn(
                name: "Picked_Event",
                table: "StatsSnapshots",
                newName: "MatchCount_Event");

            migrationBuilder.RenameColumn(
                name: "Picked_Custom",
                table: "StatsSnapshots",
                newName: "MatchCount_Custom");

            migrationBuilder.AddColumn<int>(
                name: "Banned",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchCount",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banned",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "MatchCount",
                table: "StatsSnapshots");

            migrationBuilder.RenameColumn(
                name: "MatchCount_Tournament",
                table: "StatsSnapshots",
                newName: "Picked_Tournament");

            migrationBuilder.RenameColumn(
                name: "MatchCount_Standard",
                table: "StatsSnapshots",
                newName: "Picked_Standard");

            migrationBuilder.RenameColumn(
                name: "MatchCount_Pro",
                table: "StatsSnapshots",
                newName: "Picked_Pro");

            migrationBuilder.RenameColumn(
                name: "MatchCount_Event",
                table: "StatsSnapshots",
                newName: "Picked_Event");

            migrationBuilder.RenameColumn(
                name: "MatchCount_Custom",
                table: "StatsSnapshots",
                newName: "Picked_Custom");
        }
    }
}
