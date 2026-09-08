using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Enums;

namespace DoyamoyeeMondir.Domain.Entities;

public class Expense : BaseAuditableEntity
{
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public bool RequiresApproval { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public Guid SubmissionKey { get; set; }
    public decimal PaidAmount { get; private set; }

    public void Pay(decimal amount)
    {
        if (Status != ApprovalStatus.Approved || amount <= 0 || decimal.Round(amount, 2) != amount || amount > Amount - PaidAmount)
            throw new InvalidOperationException("Only approved outstanding expenses can be paid.");
        PaidAmount += amount;
        if (PaidAmount == Amount) Status = ApprovalStatus.Paid;
    }

    public void Submit(bool requiresApproval)
    {
        if (Status != 0) throw new InvalidOperationException("Expense already submitted.");
        RequiresApproval = requiresApproval;
        Status = requiresApproval ? ApprovalStatus.Submitted : ApprovalStatus.Approved;
    }
    public void ReversePayment(decimal amount)
    {
        if (amount <= 0 || amount > PaidAmount || Status is not (ApprovalStatus.Approved or ApprovalStatus.Paid)) throw new InvalidOperationException("Invalid reversal.");
        PaidAmount -= amount; Status = ApprovalStatus.Approved;
    }
    public void CancelApproved()
    {
        if (Status != ApprovalStatus.Approved || PaidAmount != 0) throw new InvalidOperationException("Reverse payments before cancelling an expense.");
        Status = ApprovalStatus.Cancelled;
    }

    public void Review(bool approve, string userId, string? note)
    {
        if (Status != ApprovalStatus.Submitted) throw new InvalidOperationException("Expense is not pending.");
        Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        ReviewedBy = userId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note;
    }
}
