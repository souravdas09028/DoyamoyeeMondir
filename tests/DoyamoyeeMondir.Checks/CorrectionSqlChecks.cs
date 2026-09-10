using System.Security.Claims;
using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Controllers;
using DoyamoyeeMondir.Web.Models;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
internal static class CorrectionSqlChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        var original=TempleDate.Today.AddDays(-2); var corrected=TempleDate.Today;
        await using var seed=new ApplicationDbContext(options);
        var bank=new CashBankAccount { AccountNameBn="সংশোধন ব্যাংক",AccountCode="REV-B",OpeningBalance=50,OpeningBalanceDate=original,IsCashAccount=false };
        var cash=new CashBankAccount { AccountNameBn="সংশোধন নগদ",AccountCode="REV-C",OpeningBalanceDate=original };
        var cat=new IncomeCategory { NameBn="সংশোধন আয়",Code="REV" }; var expCat=new ExpenseCategory { NameBn="সংশোধন ব্যয়",Code="REV" };
        var person=new Person { NameBn="ফেরত" }; var type=new MembershipType { NameBn="ফেরত",Amount=100 };
        seed.AddRange(bank,cash,cat,expCat,person,type); await seed.SaveChangesAsync();
        var member=new Membership { PersonId=person.Id,StartDate=original,SubmissionKey=Guid.NewGuid() }; member.SetTerms(type); member.Collect(100);
        var income=new Income { Date=original,Amount=100,IncomeCategoryId=cat.Id,CategoryName=cat.NameBn,CashBankAccountId=bank.Id,AccountName=bank.AccountNameBn,PayerName=person.NameBn,Description="refund",Membership=member,SubmissionKey=Guid.NewGuid() };
        var expense=new Expense { Date=original,Amount=40,ExpenseCategoryId=expCat.Id,Description="cancel test",SubmissionKey=Guid.NewGuid() }; expense.Submit(false); expense.Pay(20);
        var payment=new ExpensePayment { Expense=expense,Date=original,Amount=20,CashBankAccountId=bank.Id,AccountName=bank.AccountNameBn,Payee="test",SubmissionKey=Guid.NewGuid() };
        var transfer=new AccountTransfer { FromAccountId=bank.Id,ToAccountId=cash.Id,Date=original,Amount=10,Description="reverse transfer",SubmissionKey=Guid.NewGuid() };
        seed.AddRange(income,payment,transfer); await seed.SaveChangesAsync();
        async Task Reverse(int kind,int id)
        {
            await using var db=new ApplicationDbContext(options); var service=new FinancialCorrections(db); var source=await service.Source(kind,id);
            await service.Reverse(new ReversalForm { SourceKind=kind,SourceId=id,Date=corrected,Reason="SQL correction test",Version=source.Version });
        }
        await Reverse(1,income.Id); await Reverse(1,income.Id);
        await using(var db=new ApplicationDbContext(options))
        {
            Assert((await db.Memberships.SingleAsync(x=>x.Id==member.Id)).CollectedAmount==0,"Income refund restores membership dues once");
            Assert(await db.FinancialReversals.CountAsync(x=>x.SourceKind==1 && x.SourceId==income.Id)==1,"Duplicate reversal creates one immutable record");
            Assert((await AccountLedger.Balances(db,original,bank.Id).SingleAsync()).Balance==120,"Historical bank balance retains original transactions");
        }
        await Reverse(2,payment.Id); await Reverse(3,transfer.Id); await Reverse(4,expense.Id);
        await using(var db=new ApplicationDbContext(options))
        {
            var e=await db.Expenses.SingleAsync(x=>x.Id==expense.Id);
            Assert(e.Status==ApprovalStatus.Cancelled && e.PaidAmount==0,"Refunded expense can be cancelled");
            Assert((await AccountLedger.Balances(db,corrected,bank.Id).SingleAsync()).Balance==50,"Reversed income payment and transfer restore account opening balance");
            Assert((await AccountLedger.Balances(db,corrected,cash.Id).SingleAsync()).Balance==0,"Transfer reversal adjusts receiving account too");
            var report=(FinancialSummary)((ViewResult)await new ReportsController(db).Index(original,original)).Model!;
            Assert(report.ExpenseHeads.Single(x=>x.Name==expCat.NameBn).Amount==40,"Historical report retains expense before cancellation date");
            var net=(FinancialSummary)((ViewResult)await new ReportsController(db).Index(original,corrected)).Model!;
            Assert(net.ExpenseHeads.Single(x=>x.Name==expCat.NameBn).Amount==0 && net.IncomeHeads.Single(x=>x.Name==cat.NameBn).Amount==0,"Range report nets dated corrections");
            var ledger=(ViewResult)await new ReconciliationController(db).Ledger(bank.Id,corrected);
            Assert(((List<LedgerRow>)ledger.Model!).Count==6,"Unified account ledger includes originals and account reversals");
        }
        ReconciliationController Controller(ApplicationDbContext db) => new(db) { ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext { User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,"SuperAdmin")],"test")) } } };
        await using(var db=new ApplicationDbContext(options))
        {
            var controller=Controller(db);
            var result=await controller.Create(new ReconciliationForm { AccountId=bank.Id,Date=corrected,StatementBalance=51,Reference="mismatch",Close=true });
            Assert(result is ViewResult && !controller.ModelState.IsValid,"Mismatched bank statement cannot close period");
        }
        var close=new ReconciliationForm { AccountId=bank.Id,Date=corrected,StatementBalance=50,Reference="matched",Close=true };
        await using(var db=new ApplicationDbContext(options)) Assert(await Controller(db).Create(close) is RedirectToActionResult,"Matching bank statement closes period");
        await using(var db=new ApplicationDbContext(options)) await Controller(db).Create(close);
        await using(var db=new ApplicationDbContext(options))
        {
            var a=await db.CashBankAccounts.SingleAsync(x=>x.Id==bank.Id);
            Assert(a.ClosedThrough==corrected && !a.CanPost(corrected) && a.CanPost(corrected.AddDays(1)),"Closed account enforces cutoff date");
            Assert(await db.BankReconciliations.CountAsync(x=>x.SubmissionKey==close.SubmissionKey)==1,"Repeated reconciliation does not create duplicates");
            var controller=new IncomeController(db); var result=await controller.Create(new IncomeForm { IncomeCategoryId=cat.Id,CashBankAccountId=bank.Id,Amount=1,Date=corrected,Description="closed" });
            Assert(result is ViewResult && !controller.ModelState.IsValid,"Income posting into closed period rejected");
        }
    }
    private static void Assert(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); Console.WriteLine("PASS: "+message); }
}

