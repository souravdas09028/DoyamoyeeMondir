using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer")]
public class AccountsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateOnly? asOf)
    {
        var date = asOf ?? TempleDate.Today; ViewBag.AsOf = date;
        return View(await DoyamoyeeMondir.Web.Services.AccountLedger.Balances(db,date).ToListAsync());
    }
    public async Task<IActionResult> Pay(int id)
    {
        var expense = await db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (expense is null) return NotFound();
        if (expense.Status != ApprovalStatus.Approved) return Conflict("ব্যয়টি পরিশোধযোগ্য নয়।");
        await Choices(); return View(new PaymentForm { ExpenseId = id, Amount = expense.Amount - expense.PaidAmount });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PaymentForm form)
    {
        var prior = await db.ExpensePayments.AsNoTracking().SingleOrDefaultAsync(x => x.SubmissionKey == form.SubmissionKey);
        if (prior != null) return RedirectToAction(nameof(Voucher), new { id = prior.Id });
        var expense = await db.Expenses.SingleOrDefaultAsync(x => x.Id == form.ExpenseId);
        var account = await db.CashBankAccounts.SingleOrDefaultAsync(x => x.Id == form.CashBankAccountId && x.IsActive);
        if (expense is null || account is null) ModelState.AddModelError("", "ব্যয় ও সক্রিয় হিসাব নির্বাচন করুন।");
        if (expense != null && (expense.Status != ApprovalStatus.Approved || form.Amount > expense.Amount - expense.PaidAmount)) ModelState.AddModelError("", "শুধু অনুমোদিত বকেয়া পরিমাণ পরিশোধ করা যাবে।");
        if (expense != null && form.Date < expense.Date || account != null && !account.CanPost(form.Date)) ModelState.AddModelError("", "ব্যয় বা হিসাব শুরুর আগের তারিখে পরিশোধ করা যাবে না।");
        if (!ModelState.IsValid) { await Choices(); return View(form); }
        expense!.Pay(form.Amount);
        var payment = new ExpensePayment { ExpenseId = expense.Id, CashBankAccountId = account!.Id, AccountName = account.AccountNameBn,
            Amount = form.Amount, Date = form.Date, Payee = form.Payee.Trim(), Reference = form.Reference?.Trim(), PaymentMethod = form.PaymentMethod, SubmissionKey = form.SubmissionKey };
        db.ExpensePayments.Add(payment);
        db.Entry(account).Property(x => x.IsActive).IsModified = true;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            prior = await db.ExpensePayments.AsNoTracking().SingleOrDefaultAsync(x => x.SubmissionKey == form.SubmissionKey);
            if (prior != null) return RedirectToAction(nameof(Voucher), new { id = prior.Id });
            ModelState.AddModelError("", "পরিশোধ সংরক্ষণ করা যায়নি। ব্যয়ের সর্বশেষ বকেয়া দেখে আবার চেষ্টা করুন।"); await Choices(); return View(form);
        }
        return RedirectToAction(nameof(Voucher), new { id = payment.Id });
    }
    public async Task<IActionResult> Voucher(int id)
    {
        var p = await db.ExpensePayments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        ViewBag.Reversal=await db.FinancialReversals.AsNoTracking().SingleOrDefaultAsync(x=>x.SourceKind==2 && x.SourceId==id);
        return p is null ? NotFound() : View(p);
    }
    public async Task<IActionResult> Payments(int? expenseId, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.ExpensePayments.AsNoTracking().Where(x => !expenseId.HasValue || x.ExpenseId == expenseId);
        ViewBag.ExpenseId = expenseId; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > page * 30;
        return View(await query.OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Transfer() { await Choices(); return View(new TransferForm()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferForm form)
    {
        if (await db.AccountTransfers.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Transfers));
        var accounts = await db.CashBankAccounts.Where(x => x.IsActive && (x.Id == form.FromAccountId || x.Id == form.ToAccountId)).ToListAsync();
        if (accounts.Count != 2 || accounts.Any(x => !x.CanPost(form.Date))) ModelState.AddModelError("", "দুটি সক্রিয় হিসাব এবং হিসাব শুরুর পরের তারিখ নির্বাচন করুন।");
        if (!ModelState.IsValid) { await Choices(); return View(form); }
        db.AccountTransfers.Add(new AccountTransfer { FromAccountId = form.FromAccountId, ToAccountId = form.ToAccountId, Date = form.Date, Amount = form.Amount, Description = form.Description.Trim(), SubmissionKey = form.SubmissionKey });
        foreach (var account in accounts) db.Entry(account).Property(x => x.IsActive).IsModified = true;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.AccountTransfers.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Transfers));
            ModelState.AddModelError("", "স্থানান্তর সংরক্ষণ করা যায়নি।"); await Choices(); return View(form);
        }
        return RedirectToAction(nameof(Transfers));
    }
    public async Task<IActionResult> Transfers(int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000); ViewBag.Page = page; ViewBag.HasNext = await db.AccountTransfers.CountAsync() > page * 30;
        return View(await db.AccountTransfers.AsNoTracking().Include(x => x.FromAccount).Include(x => x.ToAccount).OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    private async Task Choices() => ViewBag.Accounts = new SelectList(await db.CashBankAccounts.Where(x => x.IsActive).OrderBy(x => x.AccountNameBn).ToListAsync(), "Id", "AccountNameBn");
}
