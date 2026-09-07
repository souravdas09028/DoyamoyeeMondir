using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer,DonationCollector")]
public class MembershipsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(bool dueOnly = false, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.Memberships.AsNoTracking().Include(x => x.Person).AsQueryable();
        if (dueOnly) query = query.Where(x => x.CollectedAmount < x.AgreedAmount);
        ViewBag.DueOnly = dueOnly; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > (long)page * 30;
        return View(await query.OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Create(int? personId) { await Choices(); return View(new MembershipForm { PersonId = personId ?? 0 }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MembershipForm form)
    {
        if (await db.Memberships.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Index));
        var type = await db.MembershipTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == form.MembershipTypeId && x.IsActive);
        if (type is null || !await db.Persons.AnyAsync(x => x.Id == form.PersonId && x.IsActive)) ModelState.AddModelError("", "সক্রিয় ব্যক্তি ও সদস্যপদের ধরন নির্বাচন করুন।");
        if (!ModelState.IsValid) { await Choices(); return View(form); }
        var m = new Membership { PersonId = form.PersonId, StartDate = form.StartDate, EndDate = form.EndDate, SubmissionKey = form.SubmissionKey };
        m.SetTerms(type!);
        db.Memberships.Add(m);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.Memberships.AsNoTracking().AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(Index));
            ModelState.AddModelError("", "সংরক্ষণ করা যায়নি। একই ব্যক্তি, ধরন ও শুরুর তারিখের সদস্যপদ আগে যোগ করা আছে কি না দেখুন।");
            await Choices(); return View(form);
        }
        return RedirectToAction(nameof(Index));
    }
    private async Task Choices()
    {
        ViewBag.People = new SelectList(await db.Persons.Where(x => x.IsActive).OrderBy(x => x.NameBn).Select(x => new { x.Id, Name = x.NameBn + " — " + (x.MobileNumber ?? "") }).ToListAsync(), "Id", "Name");
        ViewBag.Types = new SelectList((await db.MembershipTypes.Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync()).Select(x => new { x.Id, Name = $"{x.NameBn} — {x.Amount:N2} টাকা" }), "Id", "Name");
    }
}
