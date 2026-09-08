using System.ComponentModel.DataAnnotations;
namespace DoyamoyeeMondir.Web.Models;
public static class TempleRoles
{
    public static readonly Dictionary<string,string> Labels = new()
    {
        ["SuperAdmin"]="প্রধান প্রশাসক", ["TempleAdmin"]="মন্দির প্রশাসক",
        ["Accountant"]="হিসাবরক্ষক", ["Treasurer"]="কোষাধ্যক্ষ", ["DonationCollector"]="অর্থ সংগ্রাহক",
        ["InventoryManager"]="মজুত ব্যবস্থাপক", ["DocumentManager"]="নথি ব্যবস্থাপক", ["Auditor"]="নিরীক্ষক",
        ["President"]="সভাপতি", ["GeneralSecretary"]="সাধারণ সম্পাদক", ["Viewer"]="দর্শক"
    };
}
public class UserForm : IValidatableObject
{
    public string? Id { get; set; }
    public string? Stamp { get; set; }
    [Required(ErrorMessage="নাম লিখুন।"),StringLength(150)] public string NameBn { get; set; }="";
    [Required(ErrorMessage="ইমেইল লিখুন।"),EmailAddress(ErrorMessage="সঠিক ইমেইল লিখুন।"),StringLength(256)] public string Email { get; set; }="";
    public bool IsActive { get; set; }=true;
    public List<string> Roles { get; set; }=[];
    [DataType(DataType.Password),StringLength(128,MinimumLength=8,ErrorMessage="পাসওয়ার্ড ৮ থেকে ১২৮ অক্ষরের হতে হবে।")] public string? Password { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Password)) yield return new("নতুন ব্যবহারকারীর পাসওয়ার্ড লিখুন।");
        if(Roles.Count==0 || Roles.Any(x=>!TempleRoles.Labels.ContainsKey(x))) yield return new("অন্তত একটি বৈধ ভূমিকা নির্বাচন করুন।");
    }
}
public class ResetUserPasswordForm
{
    [Required] public string Id { get; set; }="";
    [Required] public string Stamp { get; set; }="";
    [Required(ErrorMessage="নতুন পাসওয়ার্ড লিখুন।"),DataType(DataType.Password),StringLength(128,MinimumLength=8)] public string Password { get; set; }="";
    [Required,DataType(DataType.Password),Compare(nameof(Password),ErrorMessage="পাসওয়ার্ড দুটি মিলছে না।")] public string ConfirmPassword { get; set; }="";
}
public record UserListRow(string Id,string Name,string Email,bool Active,bool Locked,List<string> Roles);
