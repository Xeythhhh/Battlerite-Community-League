#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_Analytics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AverageDraftTime",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "LongestDraftLink",
                table: "Stats",
                type: "TEXT",
                nullable: false,
                defaultValue: "about:blank");

            migrationBuilder.AddColumn<long>(
                name: "LongestDraftTime",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ShortestDraftLink",
                table: "Stats",
                type: "TEXT",
                nullable: false,
                defaultValue: "about:blank");

            migrationBuilder.AddColumn<long>(
                name: "ShortestDraftTime",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TimedDrafts",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RegionDraftTimes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<string>(type: "TEXT", nullable: false),
                    TimedDrafts = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestTime = table.Column<long>(type: "INTEGER", nullable: false),
                    LongestUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ShortestTime = table.Column<long>(type: "INTEGER", nullable: false),
                    ShortestUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Average = table.Column<long>(type: "INTEGER", nullable: false),
                    LongestLink = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "about:blank"),
                    ShortestLink = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "about:blank"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionDraftTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionDraftTimes_Users_LongestUserId",
                        column: x => x.LongestUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegionDraftTimes_Users_ShortestUserId",
                        column: x => x.ShortestUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegionDraftTimes_LongestUserId",
                table: "RegionDraftTimes",
                column: "LongestUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionDraftTimes_ShortestUserId",
                table: "RegionDraftTimes",
                column: "ShortestUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionDraftTimes");

            migrationBuilder.DropColumn(
                name: "AverageDraftTime",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "LongestDraftLink",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "LongestDraftTime",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "ShortestDraftLink",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "ShortestDraftTime",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "TimedDrafts",
                table: "Stats");
        }
    }
}
