#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Add_Premade3v3_Stats_On_User : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Banned_Premade3v3",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed_Premade3v3",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon_Premade3v3",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MatchCount_Premade3v3",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banned_Premade3v3",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "GamesPlayed_Premade3v3",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "GamesWon_Premade3v3",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "MatchCount_Premade3v3",
                table: "StatsSnapshots");
        }
    }
}
