using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Domain.Entities;

public class MembershipType : BaseAuditableEntity
{
    public string NameBn { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
}
