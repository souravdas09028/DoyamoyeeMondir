using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoyamoyeeMondir.Infrastructure.Migrations;

public partial class PersonManualSerialNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SerialNumber", table: "Persons",
            type: "nvarchar(50)", maxLength: 50, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SerialNumber", table: "Persons");
    }
}
