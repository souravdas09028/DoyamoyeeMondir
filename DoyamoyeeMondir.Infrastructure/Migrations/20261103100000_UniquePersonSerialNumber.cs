using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoyamoyeeMondir.Infrastructure.Migrations;

public partial class UniquePersonSerialNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE [Persons] SET [SerialNumber] = NULL WHERE LTRIM(RTRIM([SerialNumber])) = N'';");
        migrationBuilder.AddColumn<string>(
            name: "SerialNumberKey", table: "Persons", type: "nvarchar(50)",
            maxLength: 50, nullable: true,
            computedColumnSql: "CASE WHEN NULLIF(LTRIM(RTRIM([SerialNumber])), N'') IS NULL THEN NULL ELSE RIGHT(REPLICATE(N'0', 50) + TRANSLATE(LTRIM(RTRIM([SerialNumber])), N'০১২৩৪৫৬৭৮৯', N'0123456789'), 50) END", stored: true);
        migrationBuilder.Sql(@"
IF EXISTS (SELECT [SerialNumberKey] FROM [Persons] WHERE [SerialNumber] IS NOT NULL GROUP BY [SerialNumberKey] HAVING COUNT(*) > 1)
    THROW 50001, N'Duplicate personal serial numbers exist. Assign different serial numbers before applying this migration.', 1;");
        migrationBuilder.CreateIndex(
            name: "IX_Persons_SerialNumberKey", table: "Persons",
            column: "SerialNumberKey", unique: true,
            filter: "[SerialNumber] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Persons_SerialNumberKey", table: "Persons");
        migrationBuilder.DropColumn(name: "SerialNumberKey", table: "Persons");
    }
}
