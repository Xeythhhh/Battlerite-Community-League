﻿#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    public partial class Added_BalanceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BalanceHistory",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceHistory",
                table: "Users");
        }
    }
}
