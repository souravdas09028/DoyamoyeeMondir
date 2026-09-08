using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class StockMovement : BaseAuditableEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal Change { get; set; }
    public string Reason { get; set; } = "";
    public Guid SubmissionKey { get; set; }
}
