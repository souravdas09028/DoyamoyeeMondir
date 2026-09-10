using System.ComponentModel.DataAnnotations;
namespace DoyamoyeeMondir.Web.Models;
public class ReversalForm : IValidatableObject
{
    [Range(1,4)] public int SourceKind { get; set; }
    [Range(1,int.MaxValue)] public int SourceId { get; set; }
    public DateOnly Date { get; set; }=TempleDate.Today;
    [Required(ErrorMessage="সংশোধনের কারণ লিখুন।"),StringLength(1000)] public string Reason { get; set; }="";
    [Required] public string Version { get; set; }="";
    public Guid SubmissionKey { get; set; }=Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Date==default || Date>TempleDate.Today) yield return new("সঠিক সংশোধনের তারিখ লিখুন।");
        if(SubmissionKey==Guid.Empty) yield return new("ফর্মটি আবার খুলুন।");
    }
}
public class ReconciliationForm : IValidatableObject
{
    [Range(1,int.MaxValue)] public int AccountId { get; set; }
    public DateOnly Date { get; set; }=TempleDate.Today;
    [Range(typeof(decimal),"-9999999999999999.99","9999999999999999.99")] public decimal StatementBalance { get; set; }
    [Required,StringLength(200)] public string Reference { get; set; }="";
    public bool Close { get; set; }
    public Guid SubmissionKey { get; set; }=Guid.NewGuid();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Date==default || Date>TempleDate.Today) yield return new("আজকের মধ্যে একটি বৈধ তারিখ নির্বাচন করুন।");
        if(decimal.Round(StatementBalance,2)!=StatementBalance) yield return new("সর্বোচ্চ দুই ঘর দশমিক ব্যবহার করুন।");
        if(SubmissionKey==Guid.Empty) yield return new("ফর্মটি আবার খুলুন।");
    }
}
public record ReversalSource(string Name,decimal Amount,DateOnly Date,string Version);
