#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class AddMapVariant_DisabledFlags4MapsAndChampions_ChampionRestrictions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Maps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Variant",
                table: "Maps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChampionRole",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Class",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Champions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Restrictions",
                table: "Champions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "ChampionRole",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "Class",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Champions");

            migrationBuilder.DropColumn(
                name: "Restrictions",
                table: "Champions");
        }
    }
}
