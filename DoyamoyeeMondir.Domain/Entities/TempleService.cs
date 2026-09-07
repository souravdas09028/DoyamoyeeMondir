using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Domain.Entities;

public class TempleService : BaseAuditableEntity
{
    public string NameBn { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TempleShare { get; set; }
    public decimal PriestShare { get; set; }
    public decimal StaffShare { get; set; }
    public bool IsActive { get; set; } = true;
}
