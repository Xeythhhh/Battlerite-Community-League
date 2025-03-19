#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedMatchAndDraft : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drafts_Users_Captain1Id",
                table: "Drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_Drafts_Users_Captain2Id",
                table: "Drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Maps_MapId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_MapId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Drafts_Captain1Id",
                table: "Drafts");

            migrationBuilder.DropIndex(
                name: "IX_Drafts_Captain2Id",
                table: "Drafts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Matches_MapId",
                table: "Matches",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Captain1Id",
                table: "Drafts",
                column: "Captain1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Captain2Id",
                table: "Drafts",
                column: "Captain2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drafts_Users_Captain1Id",
                table: "Drafts",
                column: "Captain1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Drafts_Users_Captain2Id",
                table: "Drafts",
                column: "Captain2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Maps_MapId",
                table: "Matches",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
