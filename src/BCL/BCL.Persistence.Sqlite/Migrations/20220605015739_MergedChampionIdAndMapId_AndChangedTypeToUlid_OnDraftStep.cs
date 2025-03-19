#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class MergedChampionIdAndMapId_AndChangedTypeToUlid_OnDraftStep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Champion1Id",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "Champion2Id",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Drafts");

            migrationBuilder.DropColumn(
                name: "IsCanceled",
                table: "Drafts");

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenId1",
                table: "DraftStep",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenId2",
                table: "DraftStep",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Season = table.Column<string>(type: "TEXT", nullable: false),
                    GamesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stats");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TokenId1",
                table: "DraftStep");

            migrationBuilder.DropColumn(
                name: "TokenId2",
                table: "DraftStep");

            migrationBuilder.AddColumn<string>(
                name: "Champion1Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Champion2Id",
                table: "DraftStep",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Drafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCanceled",
                table: "Drafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
