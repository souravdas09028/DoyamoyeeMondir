using System.Security.Claims;
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
public class ExpensesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Expenses.AsNoTracking().Include(x => x.ExpenseCategory).OrderByDescending(x => x.Id).ToListAsync());
    public async Task<IActionResult> Create() { await Categories(); return View(new ExpenseForm()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseForm form)
    {
        var category = await db.ExpenseCategories.SingleOrDefaultAsync(x => x.Id == form.ExpenseCategoryId && x.IsActive);
        if (category is null) ModelState.AddModelError("", "সক্রিয় ব্যয়ের ধরন নির্বাচন করুন।");
        if (!ModelState.IsValid) { await Categories(); return View(form); }
        if (await db.Expenses.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Index));
        var expense = new Expense { ExpenseCategoryId = category!.Id, Date = form.Date, Amount = form.Amount, Description = form.Description.Trim(), SubmissionKey = form.SubmissionKey };
        expense.Submit(category.RequiresApproval);
        db.Expenses.Add(expense);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.Expenses.AsNoTracking().AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Index));
            throw;
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,TempleAdmin")]
    public async Task<IActionResult> Review(int id, bool approve, string rowVersion, string? note)
    {
        if (note?.Length > 1000) return BadRequest();
        var expense = await db.Expenses.SingleOrDefaultAsync(x => x.Id == id);
        if (expense is null) return NotFound();
        if (expense.Status != ApprovalStatus.Submitted) return Conflict("এই ব্যয়টি ইতিমধ্যে পর্যালোচনা করা হয়েছে।");
        try { db.Entry(expense).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { return BadRequest(); }
        expense.Review(approve, User.FindFirstValue(ClaimTypes.NameIdentifier)!, note);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("তথ্য পরিবর্তিত হয়েছে। তালিকাটি আবার খুলুন।"); }
        return RedirectToAction(nameof(Index));
    }
    private async Task Categories() => ViewBag.Categories = new SelectList(await db.ExpenseCategories.Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync(), "Id", "NameBn");
}
