#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Add_EloModifiers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EloShift",
                table: "StatsSnapshots",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchId",
                table: "StatsSnapshots",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EloShift",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "StatsSnapshots");
        }
    }
}
