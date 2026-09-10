using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer,DonationCollector")]
public class IncomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, int? personId, int? membershipId, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.Incomes.AsNoTracking();
        if (from.HasValue) query = query.Where(x => x.Date >= from);
        if (to.HasValue) query = query.Where(x => x.Date <= to);
        if (personId.HasValue) query = query.Where(x => x.PersonId == personId);
        if (membershipId.HasValue) query = query.Where(x => x.MembershipId == membershipId);
        ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.PersonId = personId; ViewBag.MembershipId = membershipId;
        ViewBag.Total = await query.SumAsync(x => (decimal?)x.Amount) ?? 0;
        ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > (long)page * 30;
        return View(await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }

    public async Task<IActionResult> Create(int? membershipId, int? serviceId)
    {
        if (membershipId.HasValue && serviceId.HasValue) return BadRequest();
        var form = new IncomeForm { MembershipId = membershipId, TempleServiceId = serviceId };
        if (membershipId.HasValue)
        {
            var membership = await db.Memberships.SingleOrDefaultAsync(x => x.Id == membershipId);
            if (membership is null) return NotFound();
            form.PersonId = membership.PersonId; form.Amount = membership.Outstanding;
            form.Description = $"সদস্যপদ #{membership.Id} — {membership.TypeName}";
        }
        if (serviceId.HasValue)
        {
            var service = await db.TempleServices.SingleOrDefaultAsync(x => x.Id == serviceId && x.IsActive);
            if (service is null) return NotFound();
            form.Amount = service.Amount; form.Description = service.NameBn;
            form.ServiceVersion = Convert.ToBase64String(service.RowVersion);
        }
        await Choices(); return View(form);
    }

    public async Task<IActionResult> Services() => View(await db.TempleServices.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IncomeForm form)
    {
        var existing = await db.Incomes.AsNoTracking().SingleOrDefaultAsync(x => x.SubmissionKey == form.SubmissionKey);
        if (existing != null) return RedirectToAction(nameof(Receipt), new { id = existing.Id });
        var category = await db.IncomeCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == form.IncomeCategoryId && x.IsActive);
        var account = await db.CashBankAccounts.SingleOrDefaultAsync(x => x.Id == form.CashBankAccountId && x.IsActive);
        if (category is null || account is null) ModelState.AddModelError("", "সক্রিয় আয়ের খাত ও হিসাব নির্বাচন করুন।");
        if (account != null && !account.CanPost(form.Date)) ModelState.AddModelError("", "হিসাবের শুরুর তারিখের আগে সংগ্রহ করা যাবে না।");
        Membership? membership = null;
        if (form.MembershipId.HasValue)
        {
            membership = await db.Memberships.SingleOrDefaultAsync(x => x.Id == form.MembershipId);
            if (membership is null) ModelState.AddModelError("", "সদস্যপদ পাওয়া যায়নি।");
            else
            {
                form.PersonId = membership.PersonId;
                if (form.Date < membership.StartDate) ModelState.AddModelError("", "সদস্যপদের শুরুর তারিখের আগে সংগ্রহ করা যাবে না।");
                if (form.Amount > membership.Outstanding) ModelState.AddModelError("", $"বকেয়া {membership.Outstanding:N2} টাকার বেশি সংগ্রহ করা যাবে না।");
            }
        }
        var person = form.PersonId.HasValue ? await db.Persons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == form.PersonId) : null;
        if (form.PersonId.HasValue && person is null) ModelState.AddModelError("", "ব্যক্তির তথ্য পাওয়া যায়নি।");
        TempleService? service = null;
        if (form.TempleServiceId.HasValue)
        {
            service = await db.TempleServices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == form.TempleServiceId && x.IsActive);
            if (service is null || form.ServiceVersion != Convert.ToBase64String(service.RowVersion) || form.Amount != service.Amount)
                ModelState.AddModelError("", "সেবার মূল্য পরিবর্তিত হয়েছে অথবা সঠিক নয়। সেবার তালিকা থেকে নতুন ফর্ম খুলুন।");
        }
        if (!ModelState.IsValid) { await Choices(); return View(form); }
        var income = new Income { Date = form.Date, Amount = form.Amount, IncomeCategoryId = category!.Id, CashBankAccountId = account!.Id,
            PersonId = person?.Id, MembershipId = membership?.Id, PayerName = person?.NameBn ?? (string.IsNullOrWhiteSpace(form.PayerName) ? "নাম প্রকাশে অনিচ্ছুক" : form.PayerName.Trim()),
            CategoryName = category.NameBn, AccountName = account.AccountNameBn, Description = form.Description.Trim(),
            PaymentMethod = form.PaymentMethod, Reference = form.Reference?.Trim(), SubmissionKey = form.SubmissionKey };
        if (service != null) income.ApplyService(service);
        membership?.Collect(form.Amount);
        db.Incomes.Add(income);
        // Participate in account concurrency so opening terms cannot change during posting.
        db.Entry(account).Property(x => x.IsActive).IsModified = true;
        // EF saves the income and membership balance together in one transaction.
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception)
        {
            existing = await db.Incomes.AsNoTracking().SingleOrDefaultAsync(x => x.SubmissionKey == form.SubmissionKey);
            if (existing != null) return RedirectToAction(nameof(Receipt), new { id = existing.Id });
            ModelState.AddModelError("", exception is DbUpdateConcurrencyException ? "অন্য সংগ্রহের কারণে বকেয়া পরিবর্তিত হয়েছে। সদস্যপদের তালিকা থেকে আবার খুলুন।" : "সংগ্রহ সংরক্ষণ করা যায়নি। তথ্য যাচাই করে আবার চেষ্টা করুন।");
            await Choices(); return View(form);
        }
        return RedirectToAction(nameof(Receipt), new { id = income.Id });
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var income = await db.Incomes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        ViewBag.Reversal=await db.FinancialReversals.AsNoTracking().SingleOrDefaultAsync(x=>x.SourceKind==1 && x.SourceId==id);
        return income is null ? NotFound() : View(income);
    }
    private async Task Choices()
    {
        ViewBag.Categories = new SelectList(await db.IncomeCategories.Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync(), "Id", "NameBn");
        ViewBag.Accounts = new SelectList(await db.CashBankAccounts.Where(x => x.IsActive).OrderBy(x => x.AccountNameBn).ToListAsync(), "Id", "AccountNameBn");
        ViewBag.People = new SelectList(await db.Persons.OrderBy(x => x.NameBn).Select(x => new { x.Id, Name = x.NameBn + " — " + (x.MobileNumber ?? "") }).ToListAsync(), "Id", "Name");
    }
}
