using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Add_ProApplications_On_User : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProApplications",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProApplications",
                table: "Users");
        }
    }
}
