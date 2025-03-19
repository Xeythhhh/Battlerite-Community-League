#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddChampions_Maps_Drafts_Matches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Champions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Champions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drafts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Captain1Id = table.Column<string>(type: "TEXT", nullable: false),
                    Captain2Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drafts_Users_Captain1Id",
                        column: x => x.Captain1Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Drafts_Users_Captain2Id",
                        column: x => x.Captain2Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DraftId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "Drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DraftStep",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    ChampionId = table.Column<string>(type: "TEXT", nullable: false),
                    MapId = table.Column<string>(type: "TEXT", nullable: false),
                    DraftId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DraftStep_Champions_ChampionId",
                        column: x => x.ChampionId,
                        principalTable: "Champions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DraftStep_Drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "Drafts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DraftStep_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MatchId",
                table: "Users",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MatchId1",
                table: "Users",
                column: "MatchId1");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Captain1Id",
                table: "Drafts",
                column: "Captain1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Captain2Id",
                table: "Drafts",
                column: "Captain2Id");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_ChampionId",
                table: "DraftStep",
                column: "ChampionId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_DraftId",
                table: "DraftStep",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftStep_MapId",
                table: "DraftStep",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DraftId",
                table: "Matches",
                column: "DraftId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Matches_MatchId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Matches_MatchId1",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DraftStep");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Champions");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "Drafts");

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
        }
    }
}
