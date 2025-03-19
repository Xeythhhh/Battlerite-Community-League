#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Renamed_Membership_OnSnapshots_ToLeague : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Membership",
                table: "StatsSnapshots",
                newName: "League");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "League",
                table: "StatsSnapshots",
                newName: "Membership");
        }
    }
}
