using System.ComponentModel.DataAnnotations;

namespace DoyamoyeeMondir.Web.Models;

public class SettingForm : IValidatableObject
{
    public int Id { get; set; }
    public string Kind { get; set; } = "membership";
    [Required(ErrorMessage = "নাম লিখুন।"), StringLength(150)]
    [Display(Name = "নাম")]
    public string NameBn { get; set; } = "";
    [StringLength(30), Display(Name = "কোড")]
    public string? Code { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "সঠিক পরিমাণ লিখুন।")]
    [Display(Name = "পরিমাণ (টাকা)")]
    public decimal Amount { get; set; }
    [Display(Name = "মন্দিরের অংশ (টাকা)")]
    public decimal TempleShare { get; set; }
    [Display(Name = "পুরোহিতের অংশ (টাকা)")]
    public decimal PriestShare { get; set; }
    [Display(Name = "কর্মচারীর অংশ (টাকা)")]
    public decimal StaffShare { get; set; }
    [Display(Name = "অ্যাডমিনের অনুমোদন প্রয়োজন")]
    public bool RequiresApproval { get; set; }
    [Display(Name = "সক্রিয়")]
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
    public bool IsCashAccount { get; set; } = true;
    public DateOnly? OpeningDate { get; set; } = TempleDate.Today;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Kind is "expense" or "income" or "account" && string.IsNullOrWhiteSpace(Code))
            yield return new ValidationResult("কোড লিখুন।", [nameof(Code)]);
        if (Kind == "account" && (OpeningDate is null || OpeningDate == default(DateOnly)))
            yield return new ValidationResult("হিসাবের শুরুর তারিখ লিখুন।");
        if (Kind == "service" && (TempleShare < 0 || PriestShare < 0 || StaffShare < 0 || TempleShare + PriestShare + StaffShare != Amount))
            yield return new ValidationResult("সব অংশের যোগফল মোট পরিমাণের সমান হতে হবে এবং কোনো অংশ ঋণাত্মক হতে পারবে না।");
        foreach (var value in new[] { Amount, TempleShare, PriestShare, StaffShare })
            if (decimal.Round(value, 2) != value)
            {
                yield return new ValidationResult("টাকার পরিমাণে সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
                break;
            }
    }
}
