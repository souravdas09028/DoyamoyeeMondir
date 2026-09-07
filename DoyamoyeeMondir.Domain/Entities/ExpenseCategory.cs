using DoyamoyeeMondir.Domain.Common;

namespace DoyamoyeeMondir.Domain.Entities;

public class ExpenseCategory : BaseAuditableEntity
{
    public bool RequiresApproval { get; set; }
    public string NameBn { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
