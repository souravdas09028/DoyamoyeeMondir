using System.ComponentModel.DataAnnotations;

namespace DoyamoyeeMondir.Web.Models;

public class ExpenseForm : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "ব্যয়ের ধরন নির্বাচন করুন।")]
    public int ExpenseCategoryId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Bangladesh Standard Time"));
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "সঠিক পরিমাণ লিখুন।")]
    public decimal Amount { get; set; }
    [Required(ErrorMessage = "বিবরণ লিখুন।"), StringLength(1000)]
    public string Description { get; set; } = "";
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Date == default) yield return new ValidationResult("তারিখ লিখুন।");
        if (SubmissionKey == Guid.Empty) yield return new ValidationResult("ফর্মটি আবার খুলুন।");
        if (decimal.Round(Amount, 2) != Amount) yield return new ValidationResult("সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
    }
}
