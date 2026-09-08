using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Enums;
namespace DoyamoyeeMondir.Domain.Entities;

public class ExpensePayment : BaseAuditableEntity
{
    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;
    public int CashBankAccountId { get; set; }
    public CashBankAccount CashBankAccount { get; set; } = null!;
    public string AccountName { get; set; } = "";
    public string Payee { get; set; } = "";
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public Guid SubmissionKey { get; set; }
}
