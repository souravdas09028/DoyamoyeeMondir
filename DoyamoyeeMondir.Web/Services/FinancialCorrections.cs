using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Services;
public class FinancialCorrections(ApplicationDbContext db)
{
    public async Task<ReversalSource> Source(int kind,int id)
    {
        var (entity,name,amount,date)=await Load(kind,id);
        return new(name,amount,date,Convert.ToBase64String(entity.RowVersion));
    }
    private async Task<(BaseAuditableEntity Entity,string Name,decimal Amount,DateOnly Date)> Load(int kind,int id)
    {
        switch(kind)
        {
            case 1: var i=await db.Incomes.Include(x=>x.Membership).SingleOrDefaultAsync(x=>x.Id==id) ?? throw Missing(); return(i,i.Description,i.Amount,i.Date);
            case 2: var p=await db.ExpensePayments.Include(x=>x.Expense).SingleOrDefaultAsync(x=>x.Id==id) ?? throw Missing(); return(p,p.Payee,p.Amount,p.Date);
            case 3: var t=await db.AccountTransfers.SingleOrDefaultAsync(x=>x.Id==id) ?? throw Missing(); return(t,t.Description,t.Amount,t.Date);
            case 4: var e=await db.Expenses.Include(x=>x.ExpenseCategory).SingleOrDefaultAsync(x=>x.Id==id) ?? throw Missing(); return(e,e.Description,e.Amount,e.Date);
            default: throw Missing();
        }
    }
    public async Task Reverse(ReversalForm f)
    {
        if(await db.FinancialReversals.AnyAsync(x=>x.SourceKind==f.SourceKind && x.SourceId==f.SourceId)) return;
        var (entity,_,amount,date)=await Load(f.SourceKind,f.SourceId);
        if(f.Date<date || f.Date>TempleDate.Today || f.SubmissionKey==Guid.Empty || string.IsNullOrWhiteSpace(f.Reason)) throw new InvalidOperationException("মূল লেনদেনের পর থেকে আজকের মধ্যে তারিখ এবং কারণ লিখুন।");
        if(f.Version!=Convert.ToBase64String(entity.RowVersion)) throw new InvalidOperationException("লেনদেন পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।");
        var r=new FinancialReversal { SourceKind=f.SourceKind,SourceId=f.SourceId,Date=f.Date,Reason=f.Reason.Trim(),Amount=amount,SubmissionKey=f.SubmissionKey };
        switch(entity)
        {
            case Income i:
                r.DebitAccountId=i.CashBankAccountId; r.IncomeDelta=-amount; r.PriestDelta=-i.PriestShare; r.StaffDelta=-i.StaffShare; r.CategoryName=i.CategoryName;
                i.Membership?.Refund(amount);
                var sale=await db.PrasadSales.Include(x=>x.PrasadProduct).ThenInclude(x=>x.InventoryItem).SingleOrDefaultAsync(x=>x.IncomeId==i.Id);
                if(sale!=null)
                {
                    var item=sale.PrasadProduct.InventoryItem;
                    if(!item.IsActive || await db.StockMovements.Where(x=>x.InventoryItemId==item.Id).MaxAsync(x=>(DateOnly?)x.Date)>f.Date) throw new InvalidOperationException("প্রসাদের মজুত পণ্য সক্রিয় করুন এবং সর্বশেষ মজুত লেনদেনের পরের তারিখ দিন।");
                    item.Move(sale.Quantity);
                    db.StockMovements.Add(new StockMovement { InventoryItemId=item.Id,Date=f.Date,Change=sale.Quantity,Reason=$"প্রসাদ ফেরত; রসিদ #{i.Id}: {f.Reason}"[..Math.Min(1000,$"প্রসাদ ফেরত; রসিদ #{i.Id}: {f.Reason}".Length)],SubmissionKey=f.SubmissionKey });
                }
                break;
            case ExpensePayment p:
                var later=await db.FinancialReversals.Where(x=>x.SourceKind==2 && db.ExpensePayments.Any(y=>y.Id==x.SourceId && y.ExpenseId==p.ExpenseId)).MaxAsync(x=>(DateOnly?)x.Date);
                if(later>f.Date) throw new InvalidOperationException("সর্বশেষ পরিশোধ সংশোধনের আগে তারিখ দেওয়া যাবে না।");
                p.Expense.ReversePayment(amount); r.CreditAccountId=p.CashBankAccountId; break;
            case AccountTransfer t: r.DebitAccountId=t.ToAccountId; r.CreditAccountId=t.FromAccountId; break;
            case Expense e:
                var lastPayment=await db.ExpensePayments.Where(x=>x.ExpenseId==e.Id).MaxAsync(x=>(DateOnly?)x.Date);
                var lastReversal=await db.FinancialReversals.Where(x=>x.SourceKind==2 && db.ExpensePayments.Any(y=>y.Id==x.SourceId && y.ExpenseId==e.Id)).MaxAsync(x=>(DateOnly?)x.Date);
                if(lastPayment>f.Date || lastReversal>f.Date) throw new InvalidOperationException("পরিশোধ ও ফেরতের পরে ব্যয় বাতিল করুন।");
                if(e.PaidAmount!=0 || e.Status!=DoyamoyeeMondir.Domain.Enums.ApprovalStatus.Approved) throw new InvalidOperationException("শুধু অনুমোদিত অপরিশোধিত ব্যয় বাতিল করা যাবে। আগে সব পরিশোধ ফেরত নিন।");
                e.CancelApproved(); r.ExpenseDelta=-amount; r.CategoryName=e.ExpenseCategory.NameBn; break;
        }
        foreach(var accountId in new[]{r.DebitAccountId,r.CreditAccountId}.Where(x=>x.HasValue).Distinct())
        {
            var account=await db.CashBankAccounts.SingleAsync(x=>x.Id==accountId);
            if(!account.IsActive || !account.CanPost(f.Date)) throw new InvalidOperationException("হিসাব নিষ্ক্রিয় অথবা এই তারিখের হিসাব বন্ধ। বৈধ পরবর্তী তারিখ দিন।");
            db.Entry(account).Property(x=>x.IsActive).IsModified=true;
        }
        db.Entry(entity).Property(x=>x.IsDeleted).IsModified=true;
        db.FinancialReversals.Add(r);
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException)
        {
            if(await db.FinancialReversals.AsNoTracking().AnyAsync(x=>x.SourceKind==f.SourceKind && x.SourceId==f.SourceId)) return;
            throw new InvalidOperationException("লেনদেন বা হিসাব পরিবর্তিত হয়েছে। তালিকা থেকে আবার চেষ্টা করুন।");
        }
    }
    private static InvalidOperationException Missing()=>new("লেনদেন পাওয়া যায়নি।");
}
