using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Add_ProApplicationTimeout_On_User : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProApplicationTimeout",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProApplicationTimeout",
                table: "Users");
        }
    }
}
