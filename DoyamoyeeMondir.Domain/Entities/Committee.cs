using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class Committee : BaseAuditableEntity
{
    public string NameBn { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
}
public class CommitteeMember : BaseAuditableEntity
{
    public int CommitteeId { get; set; }
    public Committee Committee { get; set; } = null!;
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string Position { get; set; } = "";
}
