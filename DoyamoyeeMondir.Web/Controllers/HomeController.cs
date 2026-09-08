using Microsoft.AspNetCore.Mvc;

namespace DoyamoyeeMondir.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Web.Models;

public class HomeController(ApplicationDbContext db) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        if (!new[] { "SuperAdmin", "TempleAdmin", "Accountant", "Treasurer", "DonationCollector", "Auditor" }.Any(User.IsInRole))
            return View(new DashboardSummary { ShowFinance = false });
        var today = TempleDate.Today;
        return View(new DashboardSummary {
            TodayIncome = await db.Incomes.Where(x => x.Date == today).SumAsync(x => (decimal?)x.Amount) ?? 0,
            TodayExpenses = await db.Expenses.Where(x => x.Date == today && (x.Status == ApprovalStatus.Approved || x.Status == ApprovalStatus.Paid)).SumAsync(x => (decimal?)x.Amount) ?? 0,
            MemberCount = await db.Memberships.Where(x => x.StartDate <= today && (!x.EndDate.HasValue || x.EndDate >= today)).Select(x => x.PersonId).Distinct().CountAsync(),
            PendingCount = await db.Expenses.CountAsync(x => x.Status == ApprovalStatus.Submitted)
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
