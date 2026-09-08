using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class InventoryItem : BaseAuditableEntity
{
    public string NameBn { get; set; } = "";
    public string Unit { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public decimal Quantity { get; private set; }
    public decimal ReorderLevel { get; set; }
    public void Move(decimal change)
    {
        if (!IsActive || change == 0 || decimal.Round(change, 3) != change || Quantity + change < 0)
            throw new InvalidOperationException("Invalid movement or insufficient stock.");
        Quantity += change;
    }
}
