using System.ComponentModel.DataAnnotations;
using DoyamoyeeMondir.Domain.Enums;
namespace DoyamoyeeMondir.Web.Models;

public class PaymentForm : IValidatableObject
{
    public int ExpenseId { get; set; }
    [Range(1, int.MaxValue)] public int CashBankAccountId { get; set; }
    [Required(ErrorMessage = "প্রাপকের নাম লিখুন।"), StringLength(200)] public string Payee { get; set; } = "";
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal Amount { get; set; }
    [EnumDataType(typeof(PaymentMethod))] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    [StringLength(100)] public string? Reference { get; set; }
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context) => FormRules.Money(Date, Amount, SubmissionKey);
}
public class TransferForm : IValidatableObject
{
    [Range(1, int.MaxValue)] public int FromAccountId { get; set; }
    [Range(1, int.MaxValue)] public int ToAccountId { get; set; }
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal Amount { get; set; }
    [Required(ErrorMessage = "বিবরণ লিখুন।"), StringLength(1000)] public string Description { get; set; } = "";
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var error in FormRules.Money(Date, Amount, SubmissionKey)) yield return error;
        if (FromAccountId == ToAccountId) yield return new ValidationResult("দুটি আলাদা হিসাব নির্বাচন করুন।");
    }
}
public static class FormRules
{
    public static IEnumerable<ValidationResult> Money(DateOnly date, decimal amount, Guid key)
    {
        if (date == default || date > TempleDate.Today) yield return new ValidationResult("আজ বা আগের সঠিক তারিখ লিখুন।");
        if (decimal.Round(amount, 2) != amount) yield return new ValidationResult("সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
        if (key == Guid.Empty) yield return new ValidationResult("ফর্মটি আবার খুলুন।");
    }
}
public class InventoryForm : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "পণ্যের নাম লিখুন।"), StringLength(150)] public string NameBn { get; set; } = "";
    [Required(ErrorMessage = "একক লিখুন।"), StringLength(30)] public string Unit { get; set; } = "";
    [Range(typeof(decimal), "0", "999999999999.999")] public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    { if (decimal.Round(ReorderLevel, 3) != ReorderLevel) yield return new ValidationResult("সর্বোচ্চ তিন ঘর দশমিক লিখুন।"); }
}
public class StockForm : IValidatableObject
{
    public int InventoryItemId { get; set; }
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Range(typeof(decimal), "0.001", "999999999999.999")] public decimal Quantity { get; set; }
    public bool IsReceipt { get; set; } = true;
    [Required(ErrorMessage = "গ্রহণ বা ব্যবহারের কারণ লিখুন।"), StringLength(1000)] public string Reason { get; set; } = "";
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (Date == default || Date > TempleDate.Today) yield return new ValidationResult("আজ বা আগের সঠিক তারিখ লিখুন।");
        if (decimal.Round(Quantity, 3) != Quantity) yield return new ValidationResult("সর্বোচ্চ তিন ঘর দশমিক লিখুন।");
        if (SubmissionKey == Guid.Empty) yield return new ValidationResult("ফর্মটি আবার খুলুন।");
    }
}
public class AssetForm : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "সম্পদের নাম লিখুন।"), StringLength(150)] public string NameBn { get; set; } = "";
    [Required, StringLength(30)] public string Code { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Required, StringLength(200)] public string Custodian { get; set; } = "";
    [StringLength(100)] public string? Material { get; set; }
    [Range(typeof(decimal), "0", "999999999999.999")] public decimal? WeightGrams { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal? EstimatedValue { get; set; }
    public int? DonorId { get; set; }
    public DateOnly ReceivedDate { get; set; } = TempleDate.Today;
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (ReceivedDate == default || ReceivedDate > TempleDate.Today) yield return new ValidationResult("সঠিক প্রাপ্তির তারিখ লিখুন।");
        if (WeightGrams is decimal w && decimal.Round(w, 3) != w) yield return new ValidationResult("ওজনে সর্বোচ্চ তিন ঘর দশমিক লিখুন।");
        if (EstimatedValue is decimal v && decimal.Round(v, 2) != v) yield return new ValidationResult("মূল্যে সর্বোচ্চ দুই ঘর দশমিক লিখুন।");
    }
}
public class CommitteeForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string NameBn { get; set; } = "";
    public DateOnly StartDate { get; set; } = TempleDate.Today;
    public DateOnly EndDate { get; set; } = TempleDate.Today.AddYears(1);
    [StringLength(1000)] public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    { if (StartDate == default || EndDate < StartDate) yield return new ValidationResult("কমিটির মেয়াদ সঠিক নয়।"); }
}
public class CommitteeMemberForm
{
    public int CommitteeId { get; set; }
    [Range(1, int.MaxValue)] public int PersonId { get; set; }
    [Required, StringLength(100)] public string Position { get; set; } = "";
}
public class DocumentForm
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(100)] public string? Reference { get; set; }
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Required(ErrorMessage = "নথির ফাইল নির্বাচন করুন।")] public IFormFile? Upload { get; set; }
}
public record AccountBalance(int Id, string Name, decimal Opening, decimal Income, decimal Payments, decimal TransferIn, decimal TransferOut)
{ public decimal Balance => Opening + Income - Payments + TransferIn - TransferOut; }
