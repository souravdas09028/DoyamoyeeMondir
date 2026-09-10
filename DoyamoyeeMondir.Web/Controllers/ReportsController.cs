using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer,Auditor")]
public class ReportsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var end = to ?? TempleDate.Today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);
        if (start > end) { ModelState.AddModelError("", "শুরুর তারিখ শেষ তারিখের পরে হতে পারে না।"); return View(new FinancialSummary { From = start, To = end }); }
        var incomes = db.Incomes.AsNoTracking().Where(x => x.Date >= start && x.Date <= end);
        var expenses = db.Expenses.AsNoTracking().Where(x => x.Date >= start && x.Date <= end);
        var approved = expenses.Where(x => x.Status == ApprovalStatus.Approved || x.Status == ApprovalStatus.Paid || x.Status == ApprovalStatus.Cancelled);
        var reversals=await db.FinancialReversals.AsNoTracking().Where(x=>x.Date>=start && x.Date<=end).ToListAsync();
        var summary = new FinancialSummary {
            From = start, To = end,
            Income = await incomes.SumAsync(x => (decimal?)x.Amount) ?? 0,
            ApprovedExpenses = await approved.SumAsync(x => (decimal?)x.Amount) ?? 0,
            PendingExpenses = await expenses.Where(x => x.Status == ApprovalStatus.Submitted).SumAsync(x => (decimal?)x.Amount) ?? 0,
            PriestShare = await incomes.SumAsync(x => (decimal?)x.PriestShare) ?? 0,
            StaffShare = await incomes.SumAsync(x => (decimal?)x.StaffShare) ?? 0,
            IncomeHeads = await incomes.GroupBy(x => x.CategoryName).Select(g => new SummaryRow(g.Key, g.Sum(x => x.Amount))).ToListAsync(),
            ExpenseHeads = await approved.GroupBy(x => x.ExpenseCategory.NameBn).Select(g => new SummaryRow(g.Key, g.Sum(x => x.Amount))).ToListAsync()
        };
        summary.Income+=reversals.Sum(x=>x.IncomeDelta);
        summary.ApprovedExpenses+=reversals.Sum(x=>x.ExpenseDelta);
        summary.PriestShare+=reversals.Sum(x=>x.PriestDelta);
        summary.StaffShare+=reversals.Sum(x=>x.StaffDelta);
        summary.IncomeHeads=summary.IncomeHeads.Concat(reversals.Where(x=>x.IncomeDelta!=0).Select(x=>new SummaryRow(x.CategoryName,x.IncomeDelta))).GroupBy(x=>x.Name).Select(g=>new SummaryRow(g.Key,g.Sum(x=>x.Amount))).ToList();
        summary.ExpenseHeads=summary.ExpenseHeads.Concat(reversals.Where(x=>x.ExpenseDelta!=0).Select(x=>new SummaryRow(x.CategoryName,x.ExpenseDelta))).GroupBy(x=>x.Name).Select(g=>new SummaryRow(g.Key,g.Sum(x=>x.Amount))).ToList();
        return View(summary);
    }
}
