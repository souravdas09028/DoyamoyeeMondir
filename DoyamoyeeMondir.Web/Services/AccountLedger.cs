using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Services;
public static class AccountLedger
{
    public static IQueryable<AccountBalance> Balances(ApplicationDbContext db,DateOnly date,int? accountId=null) => db.CashBankAccounts.AsNoTracking().Where(a=>(!accountId.HasValue || a.Id==accountId) && (!a.OpeningBalanceDate.HasValue || a.OpeningBalanceDate<=date)).OrderBy(a=>a.AccountNameBn).Select(a=>new AccountBalance(a.Id,a.AccountNameBn,a.OpeningBalance,
        db.Incomes.Where(x=>x.CashBankAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0,
        db.ExpensePayments.Where(x=>x.CashBankAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0,
        db.AccountTransfers.Where(x=>x.ToAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0,
        db.AccountTransfers.Where(x=>x.FromAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0,
        (db.FinancialReversals.Where(x=>x.CreditAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0)-(db.FinancialReversals.Where(x=>x.DebitAccountId==a.Id && x.Date<=date).Sum(x=>(decimal?)x.Amount)??0)));
}

