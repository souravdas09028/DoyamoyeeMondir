using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer")]
public class EmployeesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Employees.AsNoTracking().OrderBy(x => x.NameBn).ToListAsync());
    [Authorize(Roles = "SuperAdmin,TempleAdmin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return View(new EmployeeForm());
        var e = await db.Employees.FindAsync(id);
        return e is null ? NotFound() : View(new EmployeeForm { Id=e.Id, NameBn=e.NameBn, Position=e.Position, Mobile=e.Mobile, MonthlySalary=e.MonthlySalary, JoinedOn=e.JoinedOn, IsActive=e.IsActive, RowVersion=Convert.ToBase64String(e.RowVersion) });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,TempleAdmin")]
    public async Task<IActionResult> Edit(EmployeeForm f)
    {
        if (!ModelState.IsValid) return View(f);
        var e = f.Id == 0 ? new Employee() : await db.Employees.FindAsync(f.Id);
        if (e is null) return NotFound();
        if (f.Id != 0)
        {
            try { db.Entry(e).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(f.RowVersion ?? ""); }
            catch (FormatException) { return BadRequest(); }
            if (e.JoinedOn != f.JoinedOn && await db.PayrollEntries.AnyAsync(x => x.EmployeeId == e.Id))
            { ModelState.AddModelError("", "বেতন নথিভুক্ত হওয়ার পরে যোগদানের তারিখ পরিবর্তন করা যাবে না।"); return View(f); }
        }
        e.NameBn=f.NameBn.Trim(); e.Position=f.Position.Trim(); e.Mobile=f.Mobile?.Trim(); e.MonthlySalary=f.MonthlySalary; e.JoinedOn=f.JoinedOn; e.IsActive=f.IsActive;
        if (f.Id == 0) db.Employees.Add(e);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("তথ্য পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।"); }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Payroll(int page = 1)
    {
        page=Math.Max(1,page); ViewBag.Page=page; ViewBag.Total=await db.PayrollEntries.CountAsync();
        return View(await db.PayrollEntries.AsNoTracking().Include(x => x.Expense).OrderByDescending(x => x.Month).ThenByDescending(x => x.Id).Skip((page-1)*50).Take(50).ToListAsync());
    }
    public async Task<IActionResult> Generate(int id)
    {
        var e=await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        if(e is null) return NotFound();
        await Choices(e); return View(new PayrollForm { EmployeeId=e.Id, EmployeeVersion=Convert.ToBase64String(e.RowVersion) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(PayrollForm f)
    {
        if(await db.Expenses.AnyAsync(x => x.SubmissionKey == f.SubmissionKey)) return RedirectToAction(nameof(Payroll));
        var e=await db.Employees.SingleOrDefaultAsync(x => x.Id == f.EmployeeId && x.IsActive);
        if(e is null) return NotFound();
        var c=await db.ExpenseCategories.SingleOrDefaultAsync(x => x.Id == f.ExpenseCategoryId && x.IsActive);
        if(c is null) ModelState.AddModelError("", "সক্রিয় ব্যয়ের ধরন নির্বাচন করুন।");
        if(f.EmployeeVersion != Convert.ToBase64String(e.RowVersion)) ModelState.AddModelError("", "কর্মীর বেতন পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।");
        if(f.Month < new DateOnly(e.JoinedOn.Year,e.JoinedOn.Month,1) || f.Date < e.JoinedOn) ModelState.AddModelError("", "যোগদানের আগের বেতন নথিভুক্ত করা যাবে না।");
        if(e.MonthlySalary + f.Allowance <= f.Deduction) ModelState.AddModelError("", "প্রদেয় বেতন শূন্যের বেশি হতে হবে।");
        if(await db.PayrollEntries.AnyAsync(x => x.EmployeeId == e.Id && x.Month == f.Month)) ModelState.AddModelError("", "এই মাসের বেতন ইতিমধ্যে নথিভুক্ত হয়েছে।");
        if(!ModelState.IsValid) { await Choices(e); return View(f); }
        var expense=new Expense { ExpenseCategoryId=c!.Id, Date=f.Date, Amount=e.MonthlySalary+f.Allowance-f.Deduction, Description=$"বেতন: {e.NameBn}, {f.Month:yyyy-MM}", SubmissionKey=f.SubmissionKey };
        expense.Submit(c.RequiresApproval);
        db.PayrollEntries.Add(new PayrollEntry { Employee=e, Month=f.Month, EmployeeName=e.NameBn, BaseSalary=e.MonthlySalary, Allowance=f.Allowance, Deduction=f.Deduction, Expense=expense });
        db.Entry(e).Property(x => x.IsActive).IsModified=true;
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateConcurrencyException) { return Conflict("কর্মীর তথ্য পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।"); }
        catch(DbUpdateException)
        {
            if(await db.PayrollEntries.AsNoTracking().AnyAsync(x => x.EmployeeId==e.Id && x.Month==f.Month)) return RedirectToAction(nameof(Payroll));
            throw;
        }
        return RedirectToAction(nameof(Payroll));
    }
    private async Task Choices(Employee e)
    {
        ViewBag.Employee=e;
        ViewBag.Categories=new SelectList(await db.ExpenseCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync(),"Id","NameBn");
    }
}
