#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_ChartSettings_OnUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Chart_MainRatingColor",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "#18FF00FF");

            migrationBuilder.AddColumn<string>(
                name: "Chart_MainWinrateColor",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "#328529B3");

            migrationBuilder.AddColumn<string>(
                name: "Chart_SecondaryRatingColor",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "#D9D500FF");

            migrationBuilder.AddColumn<string>(
                name: "Chart_SecondaryWinrateColor",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "#D9D50069");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chart_MainRatingColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Chart_MainWinrateColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Chart_SecondaryRatingColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Chart_SecondaryWinrateColor",
                table: "Users");
        }
    }
}
