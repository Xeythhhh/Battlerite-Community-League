#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddedSeasonStatsOnUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GamesWon",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Map1Id",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "Map2Id",
                table: "DraftStep");

            migrationBuilder.RenameColumn(
                name: "GamesPlayed",
                table: "Stats",
                newName: "LowestRating");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationInfo",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HighestRating",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Stats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EloShift",
                table: "Matches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "StatsSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed_Contender = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon_Contender = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed_Pro = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon_Pro = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed_Tournament = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon_Tournament = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed_Event = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon_Event = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesPlayed_Custom = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesWon_Custom = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    StatsId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatsSnapshots_Stats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "Stats",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stats_UserId",
                table: "Stats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StatsSnapshots_StatsId",
                table: "StatsSnapshots",
                column: "StatsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stats_Users_UserId",
                table: "Stats",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stats_Users_UserId",
                table: "Stats");

            migrationBuilder.DropTable(
                name: "StatsSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Stats_UserId",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "RegistrationInfo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HighestRating",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "EloShift",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "LowestRating",
                table: "Stats",
                newName: "GamesPlayed");

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Map1Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map2Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: true);
        }
    }
}
