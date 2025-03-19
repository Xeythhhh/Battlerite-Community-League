#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddedChamp2andMap2OnDraftStep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Champions_ChampionId",
                table: "DraftStep");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Maps_MapId",
                table: "DraftStep");

            migrationBuilder.RenameColumn(
                name: "MapId",
                table: "DraftStep",
                newName: "Map2Id");

            migrationBuilder.RenameColumn(
                name: "ChampionId",
                table: "DraftStep",
                newName: "Map1Id");

            migrationBuilder.RenameIndex(
                name: "IX_DraftStep_MapId",
                newName: "IX_DraftStep_Map2Id",
                table: "DraftStep");

            migrationBuilder.RenameIndex(
                name: "IX_DraftStep_ChampionId",
                newName: "IX_DraftStep_Map1Id",
                table: "DraftStep");

            migrationBuilder.AddColumn<string>(
                name: "Champion1Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Champion2Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Champion1Id",
                table: "DraftStep",
                column: "Champion1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Champion2Id",
                table: "DraftStep",
                column: "Champion2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Champions_Champion1Id",
                table: "DraftStep",
                column: "Champion1Id",
                principalTable: "Champions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Champions_Champion2Id",
                table: "DraftStep",
                column: "Champion2Id",
                principalTable: "Champions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Maps_Map1Id",
                table: "DraftStep",
                column: "Map1Id",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Maps_Map2Id",
                table: "DraftStep",
                column: "Map2Id",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Champions_Champion1Id",
                table: "DraftStep");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Champions_Champion2Id",
                table: "DraftStep");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Maps_Map1Id",
                table: "DraftStep");

            migrationBuilder.DropForeignKey(
                name: "FK_DraftStep_Maps_Map2Id",
                table: "DraftStep");

            migrationBuilder.DropIndex(
                name: "IX_DraftStep_Champion1Id",
                table: "DraftStep");

            migrationBuilder.DropIndex(
                name: "IX_DraftStep_Champion2Id",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "Champion1Id",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "Champion2Id",
                table: "DraftStep");

            migrationBuilder.RenameColumn(
                name: "Map2Id",
                table: "DraftStep",
                newName: "MapId");

            migrationBuilder.RenameColumn(
                name: "Map1Id",
                table: "DraftStep",
                newName: "ChampionId");

            migrationBuilder.RenameIndex(
                name: "IX_DraftStep_Map2Id",
                newName: "IX_DraftStep_MapId",
                table: "DraftStep");

            migrationBuilder.RenameIndex(
                name: "IX_DraftStep_Map1Id",
                newName: "IX_DraftStep_ChampionId",
                table: "DraftStep");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Champions_ChampionId",
                table: "DraftStep",
                column: "ChampionId",
                principalTable: "Champions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Maps_MapId",
                table: "DraftStep",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
