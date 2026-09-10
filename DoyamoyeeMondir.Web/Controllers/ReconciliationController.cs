using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;
[Authorize(Roles="SuperAdmin,TempleAdmin,Accountant,Treasurer,Auditor")]
public class ReconciliationController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page=1)
    {
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.HasNext=await db.BankReconciliations.CountAsync()>page*30;
        return View(await db.BankReconciliations.AsNoTracking().Include(x=>x.CashBankAccount).OrderByDescending(x=>x.Id).Skip((page-1)*30).Take(30).ToListAsync());
    }
    [Authorize(Roles="SuperAdmin,TempleAdmin,Accountant,Treasurer")]
    public async Task<IActionResult> Create() { await Choices(); return View(new ReconciliationForm()); }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="SuperAdmin,TempleAdmin,Accountant,Treasurer")]
    public async Task<IActionResult> Create(ReconciliationForm f)
    {
        if(await db.BankReconciliations.AnyAsync(x=>x.SubmissionKey==f.SubmissionKey)) return RedirectToAction(nameof(Index));
        var account=await db.CashBankAccounts.SingleOrDefaultAsync(x=>x.Id==f.AccountId && x.IsActive && !x.IsCashAccount);
        if(account is null || !account.CanPost(f.Date)) ModelState.AddModelError("","সক্রিয় ব্যাংক হিসাব ও সর্বশেষ বন্ধ সময়ের পরের তারিখ নির্বাচন করুন।");
        if(f.Close && !User.IsInRole("SuperAdmin") && !User.IsInRole("TempleAdmin")) ModelState.AddModelError("","শুধু প্রশাসক হিসাবের সময়সীমা বন্ধ করতে পারবেন।");
        if(!ModelState.IsValid) { await Choices(); return View(f); }
        var balance=(await AccountLedger.Balances(db,f.Date,f.AccountId).SingleAsync()).Balance;
        if(f.Close && balance!=f.StatementBalance) { ModelState.AddModelError("",$"অমিল রয়েছে। খাতার স্থিতি {balance:0.00} টাকা। আগে পার্থক্যের কারণ যাচাই করুন।"); await Choices(); return View(f); }
        db.BankReconciliations.Add(new BankReconciliation { CashBankAccountId=f.AccountId,Date=f.Date,StatementBalance=f.StatementBalance,BookBalance=balance,Reference=f.Reference.Trim(),IsClosed=f.Close,SubmissionKey=f.SubmissionKey });
        if(f.Close) account!.ClosedThrough=f.Date;
        db.Entry(account!).Property(x=>x.IsActive).IsModified=true;
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException)
        {
            if(await db.BankReconciliations.AsNoTracking().AnyAsync(x=>x.SubmissionKey==f.SubmissionKey)) return RedirectToAction(nameof(Index));
            ModelState.AddModelError("","হিসাব পরিবর্তিত হয়েছে। আবার স্থিতি যাচাই করুন।"); await Choices(); return View(f);
        }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Ledger(int id,DateOnly? date,int page=1)
    {
        var end=date??TempleDate.Today; var a=await db.CashBankAccounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id); if(a is null) return NotFound();
        ViewBag.Account=a; ViewBag.Date=end; ViewBag.Balance=await AccountLedger.Balances(db,end,id).SingleOrDefaultAsync();
        var rows=db.Incomes.Where(x=>x.CashBankAccountId==id && x.Date<=end).Select(x=>new { x.Date,Kind=1,x.Id,x.Description,Change=x.Amount })
            .Concat(db.ExpensePayments.Where(x=>x.CashBankAccountId==id && x.Date<=end).Select(x=>new { x.Date,Kind=2,x.Id,Description=x.Payee,Change=-x.Amount }))
            .Concat(db.AccountTransfers.Where(x=>(x.FromAccountId==id || x.ToAccountId==id) && x.Date<=end).Select(x=>new { x.Date,Kind=3,x.Id,x.Description,Change=x.ToAccountId==id?x.Amount:-x.Amount }))
            .Concat(db.FinancialReversals.Where(x=>(x.CreditAccountId==id || x.DebitAccountId==id) && x.Date<=end).Select(x=>new { x.Date,Kind=5,x.Id,Description=x.Reason,Change=x.CreditAccountId==id?x.Amount:-x.Amount }));
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.HasNext=await rows.CountAsync()>page*50;
        return View(await rows.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Kind).ThenByDescending(x=>x.Id).Skip((page-1)*50).Take(50).Select(x=>new LedgerRow(x.Date,x.Kind,x.Id,x.Description,x.Change)).ToListAsync());
    }
    private async Task Choices() => ViewBag.Accounts=new SelectList(await db.CashBankAccounts.AsNoTracking().Where(x=>x.IsActive && !x.IsCashAccount).OrderBy(x=>x.AccountNameBn).ToListAsync(),"Id","AccountNameBn");
}
public record LedgerRow(DateOnly Date,int Kind,int Id,string Description,decimal Change);

