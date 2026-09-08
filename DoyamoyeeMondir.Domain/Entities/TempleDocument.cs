using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class TempleDocument : BaseAuditableEntity
{
    public string Title { get; set; } = "";
    public string? Reference { get; set; }
    public DateOnly Date { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
}
