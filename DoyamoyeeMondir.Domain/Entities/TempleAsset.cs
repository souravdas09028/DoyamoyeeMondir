using DoyamoyeeMondir.Domain.Common;
namespace DoyamoyeeMondir.Domain.Entities;

public class TempleAsset : BaseAuditableEntity
{
    public string NameBn { get; set; } = "";
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Custodian { get; set; } = "";
    public string? Material { get; set; }
    public decimal? WeightGrams { get; set; }
    public decimal? EstimatedValue { get; set; }
    public int? DonorId { get; set; }
    public Person? Donor { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public bool IsActive { get; set; } = true;
}
