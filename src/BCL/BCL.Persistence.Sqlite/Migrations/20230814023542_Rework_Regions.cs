#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Rework_Regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //EUW to EU
            Console.WriteLine("Updating Regions...EUW to EU");
            migrationBuilder.Sql("UPDATE Matches SET Region = 0 WHERE Region = 2");
            migrationBuilder.Sql("UPDATE Users SET Server = 0 WHERE Server = 2");

            //NAE to NA
            Console.WriteLine("Updating Regions...NAE to NA");
            migrationBuilder.Sql("UPDATE Matches SET Region = 1 WHERE Region = 3");
            migrationBuilder.Sql("UPDATE Users SET Server = 1 WHERE Server = 3");

            //Reorder
            Console.WriteLine("Reordering Regions...");

            migrationBuilder.Sql("UPDATE Matches SET Region = 2 WHERE Region = 1");
            migrationBuilder.Sql("UPDATE Users SET Server = 2 WHERE Server = 1");

            migrationBuilder.Sql("UPDATE Matches SET Region = 1 WHERE Region = 0");
            migrationBuilder.Sql("UPDATE Users SET Server = 1 WHERE Server = 0");

            migrationBuilder.Sql("UPDATE Matches SET Region = 0 WHERE Region = 4");
            migrationBuilder.Sql("UPDATE Users SET Server = 0 WHERE Server = 4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
