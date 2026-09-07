using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Enums;

namespace DoyamoyeeMondir.Domain.Entities;

public class Income : BaseAuditableEntity
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public int IncomeCategoryId { get; set; }
    public IncomeCategory IncomeCategory { get; set; } = null!;
    public int CashBankAccountId { get; set; }
    public CashBankAccount CashBankAccount { get; set; } = null!;
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public int? MembershipId { get; set; }
    public Membership? Membership { get; set; }
    public int? TempleServiceId { get; private set; }
    public TempleService? TempleService { get; set; }
    public string PayerName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string? ServiceName { get; private set; }
    public decimal TempleShare { get; private set; }
    public decimal PriestShare { get; private set; }
    public decimal StaffShare { get; private set; }
    public string Description { get; set; } = "";
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public Guid SubmissionKey { get; set; }

    public void ApplyService(TempleService service)
    {
        if (TempleServiceId != null || MembershipId != null || !service.IsActive || service.Amount <= 0 ||
            service.TempleShare < 0 || service.PriestShare < 0 || service.StaffShare < 0 ||
            service.TempleShare + service.PriestShare + service.StaffShare != service.Amount)
            throw new InvalidOperationException("Invalid service allocation.");
        TempleServiceId = service.Id;
        ServiceName = service.NameBn;
        Amount = service.Amount;
        TempleShare = service.TempleShare;
        PriestShare = service.PriestShare;
        StaffShare = service.StaffShare;
    }
}
