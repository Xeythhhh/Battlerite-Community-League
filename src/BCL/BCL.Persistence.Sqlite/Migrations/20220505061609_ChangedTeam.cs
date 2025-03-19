#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class ChangedTeam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Matches_MatchId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Matches_MatchId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MatchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MatchId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatchId1",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Team1",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team2",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2",
                table: "Matches");

            migrationBuilder.AddColumn<string>(
                name: "MatchId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchId1",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MatchId",
                table: "Users",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MatchId1",
                table: "Users",
                column: "MatchId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Matches_MatchId",
                table: "Users",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Matches_MatchId1",
                table: "Users",
                column: "MatchId1",
                principalTable: "Matches",
                principalColumn: "Id");
        }
    }
}
