using System.ComponentModel.DataAnnotations;
namespace DoyamoyeeMondir.Web.Models;

public class EmployeeForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(150)] public string NameBn { get; set; } = "";
    [Required, StringLength(100)] public string Position { get; set; } = "";
    [StringLength(30)] public string? Mobile { get; set; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal MonthlySalary { get; set; }
    public DateOnly JoinedOn { get; set; } = TempleDate.Today;
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (JoinedOn == default || JoinedOn > TempleDate.Today) yield return new("যোগদানের সঠিক তারিখ লিখুন।");
        if (decimal.Round(MonthlySalary, 2) != MonthlySalary) yield return new("বেতনে সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
    }
}
public class PayrollForm : IValidatableObject
{
    [Range(1, int.MaxValue)] public int EmployeeId { get; set; }
    [Required] public string EmployeeVersion { get; set; } = "";
    [Range(1, int.MaxValue)] public int ExpenseCategoryId { get; set; }
    public DateOnly Month { get; set; } = new(TempleDate.Today.Year, TempleDate.Today.Month, 1);
    public DateOnly Date { get; set; } = TempleDate.Today;
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal Allowance { get; set; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal Deduction { get; set; }
    public Guid SubmissionKey { get; set; } = Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Month == default || Month.Day != 1 || Month > TempleDate.Today) yield return new("বেতনের মাসের প্রথম দিন নির্বাচন করুন। ভবিষ্যৎ মাস গ্রহণযোগ্য নয়।");
        if (Date < Month || Date > TempleDate.Today) yield return new("ব্যয়ের তারিখ বেতনের মাস থেকে আজকের মধ্যে হতে হবে।");
        if (SubmissionKey == Guid.Empty) yield return new("ফর্মটি আবার খুলুন।");
        if (decimal.Round(Allowance, 2) != Allowance || decimal.Round(Deduction, 2) != Deduction) yield return new("সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
    }
}
