using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class AccountTransfer : BaseAuditableEntity
{
    public int FromAccountId { get; set; }
    public CashBankAccount FromAccount { get; set; } = null!;
    public int ToAccountId { get; set; }
    public CashBankAccount ToAccount { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public Guid SubmissionKey { get; set; }
}
