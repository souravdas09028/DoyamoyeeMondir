using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;
public class Employee : BaseAuditableEntity
{
    public string NameBn { get; set; } = "";
    public string Position { get; set; } = "";
    public string? Mobile { get; set; }
    public decimal MonthlySalary { get; set; }
    public DateOnly JoinedOn { get; set; }
    public bool IsActive { get; set; } = true;
}
public class PayrollEntry : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly Month { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public string EmployeeName { get; set; } = "";
    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;
}
public class PrasadProduct : BaseAuditableEntity
{
    public string NameBn { get; set; } = "";
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
public class PrasadSale : BaseAuditableEntity
{
    public int PrasadProductId { get; set; }
    public PrasadProduct PrasadProduct { get; set; } = null!;
    public int IncomeId { get; set; }
    public Income Income { get; set; } = null!;
    public string ProductName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
public class FinancialReversal : BaseAuditableEntity
{
    // 1 income, 2 payment, 3 transfer, 4 approved unpaid expense.
    public int SourceKind { get; set; }
    public int SourceId { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Amount { get; set; }
    public int? DebitAccountId { get; set; }
    public CashBankAccount? DebitAccount { get; set; }
    public int? CreditAccountId { get; set; }
    public CashBankAccount? CreditAccount { get; set; }
    public decimal IncomeDelta { get; set; }
    public decimal ExpenseDelta { get; set; }
    public decimal PriestDelta { get; set; }
    public decimal StaffDelta { get; set; }
    public Guid SubmissionKey { get; set; }
}
public class BankReconciliation : BaseAuditableEntity
{
    public int CashBankAccountId { get; set; }
    public CashBankAccount CashBankAccount { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public bool IsClosed { get; set; }
    public string Reference { get; set; } = "";
    public Guid SubmissionKey { get; set; }
}
public class UserAudit : BaseAuditableEntity
{
    public string UserId { get; set; } = "";
    public string Action { get; set; } = "";
}
