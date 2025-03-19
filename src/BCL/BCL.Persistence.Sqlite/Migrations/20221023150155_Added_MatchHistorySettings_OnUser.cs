#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_MatchHistorySettings_OnUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MatchHistory_DisplayCustom",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MatchHistory_DisplayEvent",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MatchHistory_DisplayTournament",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchHistory_DisplayCustom",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatchHistory_DisplayEvent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatchHistory_DisplayTournament",
                table: "Users");
        }
    }
}
