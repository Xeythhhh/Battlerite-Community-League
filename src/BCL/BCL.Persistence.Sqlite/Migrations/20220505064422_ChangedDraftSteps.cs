#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedDraftSteps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_DraftStep_Map1Id",
                table: "DraftStep");

            migrationBuilder.DropIndex(
                name: "IX_DraftStep_Map2Id",
                table: "DraftStep");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Champion1Id",
                table: "DraftStep",
                column: "Champion1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Champion2Id",
                table: "DraftStep",
                column: "Champion2Id");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Map1Id",
                table: "DraftStep",
                column: "Map1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_Map2Id",
                table: "DraftStep",
                column: "Map2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Champions_Champion1Id",
                table: "DraftStep",
                column: "Champion1Id",
                principalTable: "Champions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Champions_Champion2Id",
                table: "DraftStep",
                column: "Champion2Id",
                principalTable: "Champions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Maps_Map1Id",
                table: "DraftStep",
                column: "Map1Id",
                principalTable: "Maps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftStep_Maps_Map2Id",
                table: "DraftStep",
                column: "Map2Id",
                principalTable: "Maps",
                principalColumn: "Id");
        }
    }
}
