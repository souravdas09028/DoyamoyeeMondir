using System.ComponentModel.DataAnnotations;
using DoyamoyeeMondir.Domain.Enums;

namespace DoyamoyeeMondir.Web.Models;

public static class TempleDate
{
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Bangladesh Standard Time"));
}

public class PersonForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "নাম লিখুন।"), StringLength(200)] public string NameBn { get; set; } = "";
    [StringLength(200)] public string? NameEn { get; set; }
    [StringLength(20)] public string? MobileNumber { get; set; }
    [EmailAddress(ErrorMessage = "সঠিক ইমেইল লিখুন।"), StringLength(200)] public string? Email { get; set; }
    [StringLength(1000)] public string? AddressBn { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
}

public class MembershipForm : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "ব্যক্তি নির্বাচন করুন।")] public int PersonId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "সদস্যপদের ধরন নির্বাচন করুন।")] public int MembershipTypeId { get; set; }
    public DateOnly StartDate { get; set; } = TempleDate.Today;
    public DateOnly? EndDate { get; set; }
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate == default || (EndDate.HasValue && EndDate < StartDate)) yield return new ValidationResult("সদস্যপদের তারিখ সঠিক নয়।");
        if (SubmissionKey == Guid.Empty) yield return new ValidationResult("ফর্মটি আবার খুলুন।");
    }
}

public class IncomeForm : IValidatableObject
{
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "সঠিক পরিমাণ লিখুন।")] public decimal Amount { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "আয়ের খাত নির্বাচন করুন।")] public int IncomeCategoryId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "হিসাব নির্বাচন করুন।")] public int CashBankAccountId { get; set; }
    public int? PersonId { get; set; }
    public int? MembershipId { get; set; }
    public int? TempleServiceId { get; set; }
    [StringLength(200)] public string? PayerName { get; set; }
    [Required(ErrorMessage = "বিবরণ লিখুন।"), StringLength(1000)] public string Description { get; set; } = "";
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "সঠিক পরিশোধ পদ্ধতি নির্বাচন করুন।")] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    [StringLength(100)] public string? Reference { get; set; }
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public string? ServiceVersion { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Date == default) yield return new ValidationResult("তারিখ লিখুন।");
        if (MembershipId.HasValue && TempleServiceId.HasValue) yield return new ValidationResult("একটি সংগ্রহে একটি উৎস নির্বাচন করুন।");
        if (decimal.Round(Amount, 2) != Amount) yield return new ValidationResult("সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
        if (SubmissionKey == Guid.Empty) yield return new ValidationResult("ফর্মটি আবার খুলুন।");
    }
}
