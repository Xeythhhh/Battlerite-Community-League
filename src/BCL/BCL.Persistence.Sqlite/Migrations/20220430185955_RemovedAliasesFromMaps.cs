#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class RemovedAliasesFromMaps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aliases_Maps_MapId",
                table: "Aliases");

            migrationBuilder.DropIndex(
                name: "IX_Aliases_MapId",
                table: "Aliases");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Aliases");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapId",
                table: "Aliases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_MapId",
                table: "Aliases",
                column: "MapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aliases_Maps_MapId",
                table: "Aliases",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id");
        }
    }
}
