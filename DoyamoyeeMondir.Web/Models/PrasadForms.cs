using System.ComponentModel.DataAnnotations;
using DoyamoyeeMondir.Domain.Enums;
namespace DoyamoyeeMondir.Web.Models;
public class PrasadProductForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(150)] public string NameBn { get; set; } = "";
    [Range(1,int.MaxValue)] public int InventoryItemId { get; set; }
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    { if(decimal.Round(Price,2)!=Price) yield return new("দামে সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।"); }
}
public class PrasadSaleForm : IValidatableObject
{
    [Range(1,int.MaxValue)] public int ProductId { get; set; }
    [Required] public string ProductVersion { get; set; } = "";
    [Range(1,1000000)] public int Quantity { get; set; } = 1;
    [Range(1,int.MaxValue)] public int IncomeCategoryId { get; set; }
    [Range(1,int.MaxValue)] public int CashBankAccountId { get; set; }
    [StringLength(200)] public string? PayerName { get; set; }
    [StringLength(100)] public string? Reference { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public DateOnly Date { get; set; } = TempleDate.Today;
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Date==default || Date>TempleDate.Today) yield return new("সঠিক বিক্রয়ের তারিখ লিখুন।");
        if(SubmissionKey==Guid.Empty) yield return new("ফর্মটি আবার খুলুন।");
        if(!Enum.IsDefined(PaymentMethod)) yield return new("পরিশোধের মাধ্যম নির্বাচন করুন।");
    }
}
