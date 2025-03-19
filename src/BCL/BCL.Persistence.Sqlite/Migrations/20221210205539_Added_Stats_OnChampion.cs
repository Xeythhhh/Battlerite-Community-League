#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_Stats_OnChampion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanRate",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesBanned",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesPicked",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "GamesWon",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "PickRate",
                table: "Champions");

            migrationBuilder.AddColumn<int>(
                name: "Banned_Custom",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Banned_Event",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Banned_Pro",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Banned_Standard",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Banned_Tournament",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChampionStatsId",
                table: "StatsSnapshots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "StatsSnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: "StatsSnapshot");

            migrationBuilder.AddColumn<int>(
                name: "Picked_Custom",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picked_Event",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picked_Pro",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picked_Standard",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picked_Tournament",
                table: "StatsSnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChampionStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Season = table.Column<string>(type: "TEXT", nullable: false),
                    LatestMatch = table.Column<string>(type: "TEXT", nullable: false),
                    ChampionId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionStats_Champions_ChampionId",
                        column: x => x.ChampionId,
                        principalTable: "Champions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatsSnapshots_ChampionStatsId",
                table: "StatsSnapshots",
                column: "ChampionStatsId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionStats_ChampionId",
                table: "ChampionStats",
                column: "ChampionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StatsSnapshots_ChampionStats_ChampionStatsId",
                table: "StatsSnapshots",
                column: "ChampionStatsId",
                principalTable: "ChampionStats",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatsSnapshots_ChampionStats_ChampionStatsId",
                table: "StatsSnapshots");

            migrationBuilder.DropTable(
                name: "ChampionStats");

            migrationBuilder.DropIndex(
                name: "IX_StatsSnapshots_ChampionStatsId",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Banned_Custom",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Banned_Event",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Banned_Pro",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Banned_Standard",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Banned_Tournament",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "ChampionStatsId",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Picked_Custom",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Picked_Event",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Picked_Pro",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Picked_Standard",
                table: "StatsSnapshots");

            migrationBuilder.DropColumn(
                name: "Picked_Tournament",
                table: "StatsSnapshots");

            migrationBuilder.AddColumn<string>(
                name: "BanRate",
                table: "Champions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GamesBanned",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesPicked",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesWon",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PickRate",
                table: "Champions",
                type: "TEXT",
                nullable: true);
        }
    }
}
