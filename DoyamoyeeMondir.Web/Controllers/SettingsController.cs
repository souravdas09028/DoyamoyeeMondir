using DoyamoyeeMondir.Domain.Common;
using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin")]
public class SettingsController(ApplicationDbContext db) : Controller
{
    private static bool ValidKind(string kind) => kind is "membership" or "service" or "expense" or "income" or "account";

    public async Task<IActionResult> Index(string kind = "membership")
    {
        if (!ValidKind(kind)) return NotFound();
        ViewBag.Kind = kind;
        List<SettingForm> rows;
        if (kind == "income")
            rows = (await db.IncomeCategories.AsNoTracking().OrderBy(x => x.NameBn).ToListAsync()).Select(x => new SettingForm { Id = x.Id, Kind = kind, NameBn = x.NameBn, Code = x.Code, IsActive = x.IsActive }).ToList();
        else if (kind == "account")
            rows = (await db.CashBankAccounts.AsNoTracking().OrderBy(x => x.AccountNameBn).ToListAsync()).Select(x => new SettingForm { Id = x.Id, Kind = kind, NameBn = x.AccountNameBn, Code = x.AccountCode, Amount = x.OpeningBalance, IsActive = x.IsActive }).ToList();
        else if (kind == "membership")
            rows = (await db.MembershipTypes.AsNoTracking().OrderBy(x => x.NameBn).ToListAsync()).Select(x => new SettingForm { Id = x.Id, Kind = kind, NameBn = x.NameBn, Amount = x.Amount, IsActive = x.IsActive }).ToList();
        else if (kind == "service")
            rows = (await db.TempleServices.AsNoTracking().OrderBy(x => x.NameBn).ToListAsync()).Select(x => new SettingForm { Id = x.Id, Kind = kind, NameBn = x.NameBn, Amount = x.Amount, TempleShare = x.TempleShare, PriestShare = x.PriestShare, StaffShare = x.StaffShare, IsActive = x.IsActive }).ToList();
        else
            rows = (await db.ExpenseCategories.AsNoTracking().OrderBy(x => x.NameBn).ToListAsync()).Select(x => new SettingForm { Id = x.Id, Kind = kind, NameBn = x.NameBn, Code = x.Code, RequiresApproval = x.RequiresApproval, IsActive = x.IsActive }).ToList();
        return View(rows);
    }

    public async Task<IActionResult> Edit(string kind, int id = 0)
    {
        if (!ValidKind(kind)) return NotFound();
        var form = new SettingForm { Kind = kind, Id = id };
        if (id == 0) return View(form);
        var entity = await Find(kind, id);
        if (entity is null) return NotFound();
        form.RowVersion = Convert.ToBase64String(entity.RowVersion);
        if (entity is IncomeCategory i) { form.NameBn = i.NameBn; form.Code = i.Code; form.IsActive = i.IsActive; }
        if (entity is CashBankAccount a) { form.NameBn = a.AccountNameBn; form.Code = a.AccountCode; form.Amount = a.OpeningBalance; form.OpeningDate = a.OpeningBalanceDate; form.IsCashAccount = a.IsCashAccount; form.IsActive = a.IsActive; }
        if (entity is MembershipType m) { form.NameBn = m.NameBn; form.Amount = m.Amount; form.IsActive = m.IsActive; }
        if (entity is TempleService s) { form.NameBn = s.NameBn; form.Amount = s.Amount; form.TempleShare = s.TempleShare; form.PriestShare = s.PriestShare; form.StaffShare = s.StaffShare; form.IsActive = s.IsActive; }
        if (entity is ExpenseCategory e) { form.NameBn = e.NameBn; form.Code = e.Code; form.RequiresApproval = e.RequiresApproval; form.IsActive = e.IsActive; }
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SettingForm form)
    {
        if (!ValidKind(form.Kind)) return NotFound();
        if (form.Kind == "income" && await db.IncomeCategories.IgnoreQueryFilters().AnyAsync(x => x.Code == (form.Code ?? "").Trim() && x.Id != form.Id))
            ModelState.AddModelError(nameof(form.Code), "এই কোডটি ইতিমধ্যে ব্যবহার করা হয়েছে।");
        if (form.Kind == "account" && await db.CashBankAccounts.IgnoreQueryFilters().AnyAsync(x => x.AccountCode == (form.Code ?? "").Trim() && x.Id != form.Id))
            ModelState.AddModelError(nameof(form.Code), "এই কোডটি ইতিমধ্যে ব্যবহার করা হয়েছে।");
        if (form.Kind == "expense" && await db.ExpenseCategories.IgnoreQueryFilters().AnyAsync(x => x.Code == (form.Code ?? "").Trim() && x.Id != form.Id))
            ModelState.AddModelError(nameof(form.Code), "এই কোডটি ইতিমধ্যে ব্যবহার করা হয়েছে।");
        if (!ModelState.IsValid) return View(form);
        BaseAuditableEntity? entity = form.Id == 0 ? form.Kind switch
        {
            "membership" => new MembershipType(), "service" => new TempleService(), "income" => new IncomeCategory(), "account" => new CashBankAccount(), _ => new ExpenseCategory()
        } : await Find(form.Kind, form.Id);
        if (entity is null) return NotFound();
        if (entity is CashBankAccount oldAccount && form.Id != 0 &&
            (oldAccount.OpeningBalance != form.Amount || oldAccount.OpeningBalanceDate != form.OpeningDate) &&
            (oldAccount.ClosedThrough.HasValue || await db.BankReconciliations.AnyAsync(x=>x.CashBankAccountId==form.Id) || await db.Incomes.AnyAsync(x => x.CashBankAccountId == form.Id) ||
             await db.ExpensePayments.AnyAsync(x => x.CashBankAccountId == form.Id) ||
             await db.AccountTransfers.AnyAsync(x => x.FromAccountId == form.Id || x.ToAccountId == form.Id)))
        {
            ModelState.AddModelError("", "লেনদেন রয়েছে এমন হিসাবের প্রারম্ভিক স্থিতি ও তারিখ পরিবর্তন করা যাবে না।");
            return View(form);
        }
        if (form.Id == 0) db.Add(entity);
        else
        {
            byte[] version;
            try { version = Convert.FromBase64String(form.RowVersion ?? ""); }
            catch (FormatException) { return BadRequest(); }
            db.Entry(entity).Property(x => x.RowVersion).OriginalValue = version;
        }
        if (entity is IncomeCategory i) { i.NameBn = form.NameBn.Trim(); i.Code = form.Code!.Trim(); i.IsActive = form.IsActive; }
        if (entity is CashBankAccount a) { a.AccountNameBn = form.NameBn.Trim(); a.AccountCode = form.Code!.Trim(); a.OpeningBalance = form.Amount; a.OpeningBalanceDate = form.OpeningDate; a.IsCashAccount = form.IsCashAccount; a.IsActive = form.IsActive; }
        if (entity is MembershipType m) { m.NameBn = form.NameBn.Trim(); m.Amount = form.Amount; m.IsActive = form.IsActive; }
        if (entity is TempleService s) { s.NameBn = form.NameBn.Trim(); s.Amount = form.Amount; s.TempleShare = form.TempleShare; s.PriestShare = form.PriestShare; s.StaffShare = form.StaffShare; s.IsActive = form.IsActive; }
        if (entity is ExpenseCategory e) { e.NameBn = form.NameBn.Trim(); e.Code = form.Code!.Trim(); e.RequiresApproval = form.RequiresApproval; e.IsActive = form.IsActive; }
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "অন্য ব্যবহারকারী তথ্য পরিবর্তন করেছেন। তালিকায় ফিরে সর্বশেষ তথ্য খুলুন।");
            return View(form);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "সংরক্ষণ করা যায়নি। কোড ও পরিমাণ যাচাই করে আবার চেষ্টা করুন।");
            return View(form);
        }
        TempData["SettingsSuccess"] = "তথ্য সংরক্ষণ করা হয়েছে।";
        return RedirectToAction(nameof(Index), new { kind = form.Kind });
    }

    private async Task<BaseAuditableEntity?> Find(string kind, int id) => kind switch
    {
        "membership" => await db.MembershipTypes.SingleOrDefaultAsync(x => x.Id == id),
        "service" => await db.TempleServices.SingleOrDefaultAsync(x => x.Id == id),
        "income" => await db.IncomeCategories.SingleOrDefaultAsync(x => x.Id == id),
        "account" => await db.CashBankAccounts.SingleOrDefaultAsync(x => x.Id == id),
        _ => await db.ExpenseCategories.SingleOrDefaultAsync(x => x.Id == id)
    };
}
