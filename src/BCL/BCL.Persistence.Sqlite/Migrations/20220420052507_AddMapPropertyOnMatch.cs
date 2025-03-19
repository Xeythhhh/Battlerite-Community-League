#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddMapPropertyOnMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChampionRole",
                table: "Champions",
                newName: "Role");

            migrationBuilder.AddColumn<string>(
                name: "MapId",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Restrictions",
                table: "Champions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MapId",
                table: "Matches",
                column: "MapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Maps_MapId",
                table: "Matches",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Maps_MapId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_MapId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Champions",
                newName: "ChampionRole");

            migrationBuilder.AlterColumn<string>(
                name: "Restrictions",
                table: "Champions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
