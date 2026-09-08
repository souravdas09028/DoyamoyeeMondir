using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin")]
public class CommitteesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000); ViewBag.Page = page; ViewBag.HasNext = await db.Committees.CountAsync() > page * 30;
        return View(await db.Committees.AsNoTracking().OrderByDescending(x => x.StartDate).ThenByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new CommitteeForm());
        var c = await db.Committees.FindAsync(id); if (c is null) return NotFound();
        return View(new CommitteeForm { Id = id, NameBn = c.NameBn, StartDate = c.StartDate, EndDate = c.EndDate, Notes = c.Notes, RowVersion = Convert.ToBase64String(c.RowVersion) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CommitteeForm f)
    {
        if (!ModelState.IsValid) return View(f);
        var c = f.Id == 0 ? new Committee() : await db.Committees.SingleOrDefaultAsync(x => x.Id == f.Id); if (c is null) return NotFound();
        if (f.Id == 0) db.Add(c);
        else { try { db.Entry(c).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(f.RowVersion ?? ""); } catch (FormatException) { return BadRequest(); } }
        c.NameBn = f.NameBn.Trim(); c.StartDate = f.StartDate; c.EndDate = f.EndDate; c.Notes = f.Notes?.Trim();
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "তথ্য পরিবর্তিত হয়েছে। তালিকা থেকে আবার খুলুন।"); return View(f); }
        return RedirectToAction(nameof(Members), new { id = c.Id });
    }
    public async Task<IActionResult> Members(int id)
    {
        if (!await LoadMembers(id)) return NotFound(); return View(new CommitteeMemberForm { CommitteeId = id });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Members(CommitteeMemberForm f)
    {
        if (!await LoadMembers(f.CommitteeId)) return NotFound();
        if (!await db.Persons.AnyAsync(x => x.Id == f.PersonId && x.IsActive)) ModelState.AddModelError("", "সক্রিয় ব্যক্তি নির্বাচন করুন।");
        if (await db.CommitteeMembers.AnyAsync(x => x.CommitteeId == f.CommitteeId && x.PersonId == f.PersonId)) ModelState.AddModelError("", "ব্যক্তিটি ইতিমধ্যে এই কমিটিতে আছেন।");
        if (!ModelState.IsValid) return View(f);
        db.CommitteeMembers.Add(new CommitteeMember { CommitteeId = f.CommitteeId, PersonId = f.PersonId, Position = f.Position.Trim() });
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { ModelState.AddModelError("", "সদস্য যোগ করা যায়নি। সর্বশেষ তালিকা যাচাই করুন।"); return View(f); }
        return RedirectToAction(nameof(Members), new { id = f.CommitteeId });
    }
    private async Task<bool> LoadMembers(int id)
    {
        var c = await db.Committees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (c is null) return false;
        ViewBag.Committee = c;
        ViewBag.Members = await db.CommitteeMembers.AsNoTracking().Include(x => x.Person).Where(x => x.CommitteeId == id).OrderBy(x => x.Id).ToListAsync();
        ViewBag.People = new SelectList(await db.Persons.Where(x => x.IsActive).OrderBy(x => x.NameBn).ToListAsync(), "Id", "NameBn"); return true;
    }
}
