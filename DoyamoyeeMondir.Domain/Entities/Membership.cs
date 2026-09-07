using System.ComponentModel.DataAnnotations.Schema;
using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Domain.Entities;

public class Membership : BaseAuditableEntity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public int MembershipTypeId { get; set; }
    public MembershipType MembershipType { get; set; } = null!;
    public string TypeName { get; private set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal AgreedAmount { get; private set; }
    public decimal CollectedAmount { get; private set; }
    public Guid SubmissionKey { get; set; }
    [NotMapped] public decimal Outstanding => AgreedAmount - CollectedAmount;

    public void SetTerms(MembershipType type)
    {
        if (!string.IsNullOrEmpty(TypeName)) throw new InvalidOperationException("Terms already set.");
        if (!type.IsActive || type.Amount < 0) throw new InvalidOperationException("Invalid membership type.");
        MembershipTypeId = type.Id;
        TypeName = type.NameBn;
        AgreedAmount = type.Amount;
    }

    public void Collect(decimal amount)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount || amount > Outstanding)
            throw new InvalidOperationException("Collection exceeds outstanding amount or is invalid.");
        CollectedAmount += amount;
    }
}
