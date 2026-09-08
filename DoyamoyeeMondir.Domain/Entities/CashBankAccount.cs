using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Domain.Entities;

public class CashBankAccount : BaseAuditableEntity
{
    public string AccountNameBn { get; set; } = string.Empty;

    public string? AccountNameEn { get; set; }

    public string AccountCode { get; set; } = string.Empty;

    public bool IsCashAccount { get; set; }

    public string? BankName { get; set; }

    public string? BranchName { get; set; }

    public string? AccountNumber { get; set; }

    public string? MobileBankingNumber { get; set; }

    public decimal OpeningBalance { get; set; }

    public DateOnly? OpeningBalanceDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateOnly? ClosedThrough { get; set; }
    public bool CanPost(DateOnly date) => (!OpeningBalanceDate.HasValue || date >= OpeningBalanceDate) && (!ClosedThrough.HasValue || date > ClosedThrough);
}
