using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoyamoyeeMondir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PrasadSalesWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrasadProducts_InventoryItems_InventoryItemId",
                table: "PrasadProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_PrasadSales_Incomes_IncomeId",
                table: "PrasadSales");

            migrationBuilder.DropForeignKey(
                name: "FK_PrasadSales_PrasadProducts_PrasadProductId",
                table: "PrasadSales");

            migrationBuilder.DropIndex(
                name: "IX_PrasadSales_IncomeId",
                table: "PrasadSales");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "PrasadSales",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(name: "RowVersion", table: "PrasadSales");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PrasadSales", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "PrasadSales",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(name: "RowVersion", table: "PrasadProducts");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PrasadProducts", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "NameBn",
                table: "PrasadProducts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PrasadSales_IncomeId",
                table: "PrasadSales",
                column: "IncomeId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrasadSale_Amount",
                table: "PrasadSales",
                sql: "[Quantity] > 0 AND [UnitPrice] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Prasad_Price",
                table: "PrasadProducts",
                sql: "[Price] > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadProducts_InventoryItems_InventoryItemId",
                table: "PrasadProducts",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadSales_Incomes_IncomeId",
                table: "PrasadSales",
                column: "IncomeId",
                principalTable: "Incomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadSales_PrasadProducts_PrasadProductId",
                table: "PrasadSales",
                column: "PrasadProductId",
                principalTable: "PrasadProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrasadProducts_InventoryItems_InventoryItemId",
                table: "PrasadProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_PrasadSales_Incomes_IncomeId",
                table: "PrasadSales");

            migrationBuilder.DropForeignKey(
                name: "FK_PrasadSales_PrasadProducts_PrasadProductId",
                table: "PrasadSales");

            migrationBuilder.DropIndex(
                name: "IX_PrasadSales_IncomeId",
                table: "PrasadSales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrasadSale_Amount",
                table: "PrasadSales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Prasad_Price",
                table: "PrasadProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "PrasadSales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.DropColumn(name: "RowVersion", table: "PrasadSales");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PrasadSales", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "PrasadSales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.DropColumn(name: "RowVersion", table: "PrasadProducts");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PrasadProducts", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "NameBn",
                table: "PrasadProducts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.CreateIndex(
                name: "IX_PrasadSales_IncomeId",
                table: "PrasadSales",
                column: "IncomeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadProducts_InventoryItems_InventoryItemId",
                table: "PrasadProducts",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadSales_Incomes_IncomeId",
                table: "PrasadSales",
                column: "IncomeId",
                principalTable: "Incomes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrasadSales_PrasadProducts_PrasadProductId",
                table: "PrasadSales",
                column: "PrasadProductId",
                principalTable: "PrasadProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

