using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoyamoyeeMondir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinancialCorrectionsAndReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankReconciliations_CashBankAccounts_CashBankAccountId",
                table: "BankReconciliations");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_CreditAccountId",
                table: "FinancialReversals");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_DebitAccountId",
                table: "FinancialReversals");

            migrationBuilder.DropColumn(name: "RowVersion", table: "FinancialReversals");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "FinancialReversals", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "FinancialReversals",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "FinancialReversals",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(name: "RowVersion", table: "BankReconciliations");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "BankReconciliations", type: "rowversion", rowVersion: true, nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "BankReconciliations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReversals_SourceKind_SourceId",
                table: "FinancialReversals",
                columns: new[] { "SourceKind", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReversals_SubmissionKey",
                table: "FinancialReversals",
                column: "SubmissionKey",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reversal_Source",
                table: "FinancialReversals",
                sql: "[SourceKind] BETWEEN 1 AND 4 AND [SourceId] > 0 AND [Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliations_SubmissionKey",
                table: "BankReconciliations",
                column: "SubmissionKey",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reconciliation_Closed",
                table: "BankReconciliations",
                sql: "[IsClosed] = 0 OR [StatementBalance] = [BookBalance]");

            migrationBuilder.AddForeignKey(
                name: "FK_BankReconciliations_CashBankAccounts_CashBankAccountId",
                table: "BankReconciliations",
                column: "CashBankAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_CreditAccountId",
                table: "FinancialReversals",
                column: "CreditAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_DebitAccountId",
                table: "FinancialReversals",
                column: "DebitAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankReconciliations_CashBankAccounts_CashBankAccountId",
                table: "BankReconciliations");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_CreditAccountId",
                table: "FinancialReversals");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_DebitAccountId",
                table: "FinancialReversals");

            migrationBuilder.DropIndex(
                name: "IX_FinancialReversals_SourceKind_SourceId",
                table: "FinancialReversals");

            migrationBuilder.DropIndex(
                name: "IX_FinancialReversals_SubmissionKey",
                table: "FinancialReversals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reversal_Source",
                table: "FinancialReversals");

            migrationBuilder.DropIndex(
                name: "IX_BankReconciliations_SubmissionKey",
                table: "BankReconciliations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reconciliation_Closed",
                table: "BankReconciliations");

            migrationBuilder.DropColumn(name: "RowVersion", table: "FinancialReversals");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "FinancialReversals", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "FinancialReversals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "FinancialReversals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.DropColumn(name: "RowVersion", table: "BankReconciliations");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "BankReconciliations", type: "varbinary(max)", nullable: false, defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "BankReconciliations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_BankReconciliations_CashBankAccounts_CashBankAccountId",
                table: "BankReconciliations",
                column: "CashBankAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_CreditAccountId",
                table: "FinancialReversals",
                column: "CreditAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialReversals_CashBankAccounts_DebitAccountId",
                table: "FinancialReversals",
                column: "DebitAccountId",
                principalTable: "CashBankAccounts",
                principalColumn: "Id");
        }
    }
}

