using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,Accountant,Treasurer,DonationCollector")]
public class PeopleController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.Persons.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.NameBn.Contains(q) || (x.NameEn != null && x.NameEn.Contains(q)) || (x.MobileNumber != null && x.MobileNumber.Contains(q)));
        ViewBag.Q = q; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > (long)page * 30;
        return View(await query.OrderBy(x => x.NameBn).ThenBy(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }

    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new PersonForm());
        var p = await db.Persons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View(new PersonForm { Id = p.Id, NameBn = p.NameBn, NameEn = p.NameEn, MobileNumber = p.MobileNumber, Email = p.Email, AddressBn = p.AddressBn, IsActive = p.IsActive, RowVersion = Convert.ToBase64String(p.RowVersion) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PersonForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var p = form.Id == 0 ? new Person() : await db.Persons.SingleOrDefaultAsync(x => x.Id == form.Id);
        if (p is null) return NotFound();
        if (form.Id == 0) db.Persons.Add(p);
        else
        {
            try { db.Entry(p).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(form.RowVersion ?? ""); }
            catch (FormatException) { return BadRequest(); }
        }
        p.NameBn = form.NameBn.Trim(); p.NameEn = form.NameEn?.Trim(); p.MobileNumber = form.MobileNumber?.Trim();
        p.Email = form.Email?.Trim(); p.AddressBn = form.AddressBn?.Trim(); p.IsActive = form.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "তথ্য পরিবর্তিত হয়েছে। তালিকা থেকে আবার খুলুন।"); return View(form);
        }
        return RedirectToAction(nameof(Index));
    }
}
