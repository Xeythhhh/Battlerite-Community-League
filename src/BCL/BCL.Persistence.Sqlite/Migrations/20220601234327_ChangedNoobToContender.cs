#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedNoobToContender : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NoobQueue",
                table: "Users",
                newName: "ContenderQueue");

            migrationBuilder.RenameColumn(
                name: "Noob",
                table: "Users",
                newName: "Contender");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContenderQueue",
                table: "Users",
                newName: "NoobQueue");

            migrationBuilder.RenameColumn(
                name: "Contender",
                table: "Users",
                newName: "Noob");
        }
    }
}
