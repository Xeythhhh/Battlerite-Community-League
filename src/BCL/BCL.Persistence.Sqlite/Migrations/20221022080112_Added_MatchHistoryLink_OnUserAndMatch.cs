#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_MatchHistoryLink_OnUserAndMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LatestMatchLink",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "about:blank");

            migrationBuilder.AddColumn<string>(
                name: "LatestMatch_DiscordLink_Label",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LatestMatch_DiscordLink_ToolTip",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JumpLink",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "about:blank");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestMatchLink",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LatestMatch_DiscordLink_Label",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LatestMatch_DiscordLink_ToolTip",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JumpLink",
                table: "Matches");
        }
    }
}
