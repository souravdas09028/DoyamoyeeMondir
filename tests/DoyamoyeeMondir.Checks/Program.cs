using System.ComponentModel.DataAnnotations;
using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Web.Models;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
static bool Valid(object model) => Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
var immediate = new Expense();
immediate.Submit(false);
Check(immediate.Status == ApprovalStatus.Approved, "No-approval expense is recorded immediately");
var pending = new Expense();
pending.Submit(true);
Check(pending.Status == ApprovalStatus.Submitted && pending.RequiresApproval, "Approval-required expense remains pending");
pending.Review(true, "admin", "checked");
Check(pending.Status == ApprovalStatus.Approved && pending.ReviewedBy == "admin" && pending.ReviewedAt != null, "Approval records reviewer and time");
bool prevented = false;
try { pending.Review(false, "admin", null); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Already reviewed expense cannot be reviewed again");
var rejected = new Expense(); rejected.Submit(true); rejected.Review(false, "admin", "incorrect");
Check(rejected.Status == ApprovalStatus.Rejected, "Rejection remains outside approved totals");
prevented = false;
try { rejected.Submit(false); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Submission cannot overwrite historical approval requirement");
var service = new SettingForm { Kind = "service", NameBn = "পূজা", Amount = 100, TempleShare = 60, PriestShare = 30, StaffShare = 10 };
Check(Valid(service), "Valid service allocation is accepted");
service.StaffShare = 20;
Check(!Valid(service), "Unbalanced service allocation is rejected");
service.StaffShare = -10; service.TempleShare = 80;
Check(!Valid(service), "Negative allocation is rejected even when total matches");
Check(!Valid(new SettingForm { Kind = "membership", NameBn = "সদস্য", Amount = -1 }), "Negative membership amount is rejected");
Check(!Valid(new ExpenseForm { ExpenseCategoryId = 1, Amount = 1.001m, Description = "test" }), "Fractional paisa is rejected");
Check(!Valid(new ExpenseForm { ExpenseCategoryId = 1, Amount = 0, Description = "test" }), "Zero expense is rejected");

var membershipType = new MembershipType { Id = 1, NameBn = "বার্ষিক", Amount = 1000 };
var enrollment = new Membership(); enrollment.SetTerms(membershipType);
membershipType.Amount = 2000; membershipType.NameBn = "নতুন নাম";
Check(enrollment.AgreedAmount == 1000 && enrollment.TypeName == "বার্ষিক", "Membership terms survive later configuration edits");
enrollment.Collect(400);
Check(enrollment.Outstanding == 600, "Partial collection reduces outstanding dues");
prevented = false;
try { enrollment.Collect(601); } catch (InvalidOperationException) { prevented = true; }
Check(prevented && enrollment.CollectedAmount == 400, "Overpayment is rejected without changing balance");
enrollment.Collect(600);
Check(enrollment.Outstanding == 0, "Final installment settles membership");
prevented = false;
try { enrollment.SetTerms(membershipType); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Existing membership terms cannot be replaced");
var configured = new TempleService { Id = 1, NameBn = "পূজা", Amount = 100, TempleShare = 60, PriestShare = 30, StaffShare = 10 };
var receipt = new Income(); receipt.ApplyService(configured);
configured.Amount = 200; configured.TempleShare = 160;
Check(receipt.Amount == 100 && receipt.TempleShare == 60 && receipt.PriestShare == 30 && receipt.StaffShare == 10, "Service receipt preserves historical price and shares");
prevented = false;
try { new Income { MembershipId = 1 }.ApplyService(configured); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Collection cannot combine membership and service sources");
Check(!Valid(new MembershipForm { PersonId = 1, MembershipTypeId = 1, StartDate = new DateOnly(2026, 9, 8), EndDate = new DateOnly(2026, 9, 7) }), "Membership end date cannot precede start date");
Check(!Valid(new IncomeForm { IncomeCategoryId = 1, CashBankAccountId = 1, Amount = 10, Description = "test", PaymentMethod = (PaymentMethod)999 }), "Unknown payment method is rejected");
Check(!Valid(new IncomeForm { IncomeCategoryId = 1, CashBankAccountId = 1, Amount = 10, Description = "test", MembershipId = 1, TempleServiceId = 1 }), "Conflicting collection sources are rejected");
Check(!Valid(new SettingForm { Kind = "income", NameBn = "আয়" }), "Income category requires a code");
var payable = new Expense { Amount = 100 }; payable.Submit(false); payable.Pay(40);
Check(payable.PaidAmount == 40 && payable.Status == ApprovalStatus.Approved, "Partial expense payment retains outstanding approval");
prevented = false; try { payable.Pay(61); } catch (InvalidOperationException) { prevented = true; }
Check(prevented && payable.PaidAmount == 40, "Expense overpayment is rejected without mutation");
payable.Pay(60); Check(payable.Status == ApprovalStatus.Paid, "Final payment marks expense paid");
prevented = false; try { payable.Pay(1); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Paid expense cannot be paid again");
var awaiting = new Expense { Amount = 100 }; awaiting.Submit(true);
prevented = false; try { awaiting.Pay(10); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Unapproved expense cannot be paid");
var rice = new InventoryItem { NameBn = "চাল", Unit = "কেজি" }; rice.Move(10.5m); rice.Move(-3.25m);
Check(rice.Quantity == 7.25m, "Stock receipts and issues preserve fractional quantities");
prevented = false; try { rice.Move(-8); } catch (InvalidOperationException) { prevented = true; }
Check(prevented && rice.Quantity == 7.25m, "Stock cannot become negative");
rice.IsActive = false; prevented = false; try { rice.Move(1); } catch (InvalidOperationException) { prevented = true; }
Check(prevented, "Inactive inventory cannot receive movements");
Check(!Valid(new TransferForm { FromAccountId = 1, ToAccountId = 1, Amount = 10, Description = "test" }), "Transfer cannot use the same account twice");
Check(!Valid(new CommitteeForm { NameBn = "কমিটি", StartDate = new DateOnly(2026, 9, 8), EndDate = new DateOnly(2026, 9, 7) }), "Committee date range is validated");
Check(DoyamoyeeMondir.Web.Services.DocumentFiles.Detect(System.Text.Encoding.UTF8.GetBytes("<script>bad</script>")) == null, "Unsupported document content is rejected");
Check(DoyamoyeeMondir.Web.Services.DocumentFiles.Detect("%PDF-1.4\n"u8.ToArray())?.ContentType == "application/pdf", "PDF signature selects fixed download MIME type");
Check(DoyamoyeeMondir.Web.Services.DocumentFiles.Detect(new byte[DoyamoyeeMondir.Web.Services.DocumentFiles.MaxBytes + 1]) == null, "Oversized document rejected");
if (args.Contains("--sql")) await SqlChecks.Run();
