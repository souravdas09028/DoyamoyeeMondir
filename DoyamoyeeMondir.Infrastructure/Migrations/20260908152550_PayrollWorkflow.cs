using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoyamoyeeMondir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PayrollWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollEntries_Employees_EmployeeId",
                table: "PayrollEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollEntries_Expenses_ExpenseId",
                table: "PayrollEntries");

            migrationBuilder.DropIndex(
                name: "IX_PayrollEntries_EmployeeId",
                table: "PayrollEntries");

            migrationBuilder.DropIndex(
                name: "IX_PayrollEntries_ExpenseId",
                table: "PayrollEntries");

            migrationBuilder.DropColumn(name: "RowVersion", table: "PayrollEntries");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PayrollEntries", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "PayrollEntries",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(name: "RowVersion", table: "Employees");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "Employees", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NameBn",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_EmployeeId_Month",
                table: "PayrollEntries",
                columns: new[] { "EmployeeId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_ExpenseId",
                table: "PayrollEntries",
                column: "ExpenseId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payroll_Amount",
                table: "PayrollEntries",
                sql: "DAY([Month]) = 1 AND [BaseSalary] >= 0 AND [Allowance] >= 0 AND [Deduction] >= 0 AND [BaseSalary] + [Allowance] > [Deduction]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employee_Salary",
                table: "Employees",
                sql: "[MonthlySalary] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollEntries_Employees_EmployeeId",
                table: "PayrollEntries",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollEntries_Expenses_ExpenseId",
                table: "PayrollEntries",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollEntries_Employees_EmployeeId",
                table: "PayrollEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollEntries_Expenses_ExpenseId",
                table: "PayrollEntries");

            migrationBuilder.DropIndex(
                name: "IX_PayrollEntries_EmployeeId_Month",
                table: "PayrollEntries");

            migrationBuilder.DropIndex(
                name: "IX_PayrollEntries_ExpenseId",
                table: "PayrollEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payroll_Amount",
                table: "PayrollEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employee_Salary",
                table: "Employees");

            migrationBuilder.DropColumn(name: "RowVersion", table: "PayrollEntries");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PayrollEntries", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "PayrollEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.DropColumn(name: "RowVersion", table: "Employees");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "Employees", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "NameBn",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_EmployeeId",
                table: "PayrollEntries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_ExpenseId",
                table: "PayrollEntries",
                column: "ExpenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollEntries_Employees_EmployeeId",
                table: "PayrollEntries",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollEntries_Expenses_ExpenseId",
                table: "PayrollEntries",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

