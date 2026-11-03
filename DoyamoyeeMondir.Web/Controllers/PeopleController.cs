using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
        return View(await query.OrderBy(x => x.SerialNumberKey == null).ThenBy(x => x.SerialNumberKey).ThenBy(x => x.NameBn).ThenBy(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }

    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new PersonForm());
        var p = await db.Persons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View(new PersonForm { Id = p.Id, SerialNumber = p.SerialNumber, NameBn = p.NameBn, NameEn = p.NameEn, FatherName = p.FatherName, MotherName = p.MotherName, MobileNumber = p.MobileNumber, Email = p.Email, AddressBn = p.AddressBn, IsActive = p.IsActive, RowVersion = Convert.ToBase64String(p.RowVersion) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PersonForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var serialKey = BengaliDigits.SerialKey(form.SerialNumber);
        if (serialKey != null && await db.Persons.IgnoreQueryFilters().AnyAsync(x => x.Id != form.Id && x.SerialNumberKey == serialKey))
        {
            ModelState.AddModelError(nameof(form.SerialNumber), "এই ক্রমিক নম্বরটি অন্য ব্যক্তির জন্য ব্যবহার করা হয়েছে। আলাদা নম্বর লিখুন।");
            return View(form);
        }
        var p = form.Id == 0 ? new Person() : await db.Persons.SingleOrDefaultAsync(x => x.Id == form.Id);
        if (p is null) return NotFound();
        if (form.Id == 0) db.Persons.Add(p);
        else
        {
            try { db.Entry(p).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(form.RowVersion ?? ""); }
            catch (FormatException) { return BadRequest(); }
        }
        p.SerialNumber = string.IsNullOrWhiteSpace(form.SerialNumber) ? null : form.SerialNumber.Trim();
        p.NameBn = form.NameBn.Trim(); p.NameEn = form.NameEn?.Trim(); p.MobileNumber = form.MobileNumber?.Trim();
        p.FatherName = form.FatherName?.Trim(); p.MotherName = form.MotherName?.Trim();
        p.Email = form.Email?.Trim(); p.AddressBn = form.AddressBn?.Trim(); p.IsActive = form.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(form.SerialNumber), "এই ক্রমিক নম্বরটি অন্য ব্যক্তির জন্য ব্যবহার করা হয়েছে। আলাদা নম্বর লিখুন।");
            return View(form);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "তথ্য পরিবর্তিত হয়েছে। তালিকা থেকে আবার খুলুন।"); return View(form);
        }
        return RedirectToAction(nameof(Index));
    }
}
