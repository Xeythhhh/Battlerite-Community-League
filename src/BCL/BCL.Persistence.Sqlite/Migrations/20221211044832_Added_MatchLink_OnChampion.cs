#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_MatchLink_OnChampion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestMatch",
                table: "ChampionStats");

            migrationBuilder.AddColumn<string>(
                name: "LatestMatch",
                table: "Champions",
                type: "TEXT",
                nullable: false,
                defaultValue: "about:blank");

            migrationBuilder.AddColumn<string>(
                name: "LatestMatch_Label",
                table: "Champions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestMatch",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "LatestMatch_Label",
                table: "Champions");

            migrationBuilder.AddColumn<string>(
                name: "LatestMatch",
                table: "ChampionStats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
