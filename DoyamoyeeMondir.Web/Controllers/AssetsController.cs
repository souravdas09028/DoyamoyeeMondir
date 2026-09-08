using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin")]
public class AssetsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.TempleAssets.AsNoTracking().Include(x => x.Donor).Where(x => q == null || x.NameBn.Contains(q) || x.Code.Contains(q));
        ViewBag.Q = q; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > page * 30;
        return View(await query.OrderBy(x => x.Code).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Edit(int id = 0)
    {
        await Choices(); if (id == 0) return View(new AssetForm());
        var a = await db.TempleAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (a is null) return NotFound();
        return View(new AssetForm { Id = id, NameBn = a.NameBn, Code = a.Code, Description = a.Description, Location = a.Location, Custodian = a.Custodian, Material = a.Material, WeightGrams = a.WeightGrams, EstimatedValue = a.EstimatedValue, DonorId = a.DonorId, ReceivedDate = a.ReceivedDate, IsActive = a.IsActive, RowVersion = Convert.ToBase64String(a.RowVersion) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssetForm f)
    {
        if (f.DonorId.HasValue && !await db.Persons.AnyAsync(x => x.Id == f.DonorId)) ModelState.AddModelError("", "দাতার তথ্য পাওয়া যায়নি।");
        if (await db.TempleAssets.IgnoreQueryFilters().AnyAsync(x => x.Code == (f.Code ?? "").Trim() && x.Id != f.Id)) ModelState.AddModelError("", "সম্পদের কোডটি ইতিমধ্যে ব্যবহার হয়েছে।");
        if (!ModelState.IsValid) { await Choices(); return View(f); }
        var a = f.Id == 0 ? new TempleAsset() : await db.TempleAssets.SingleOrDefaultAsync(x => x.Id == f.Id); if (a is null) return NotFound();
        if (f.Id == 0) db.Add(a);
        else { try { db.Entry(a).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(f.RowVersion ?? ""); } catch (FormatException) { return BadRequest(); } }
        a.NameBn = f.NameBn.Trim(); a.Code = f.Code.Trim(); a.Description = f.Description.Trim(); a.Location = f.Location.Trim(); a.Custodian = f.Custodian.Trim(); a.Material = f.Material?.Trim(); a.WeightGrams = f.WeightGrams; a.EstimatedValue = f.EstimatedValue; a.DonorId = f.DonorId; a.ReceivedDate = f.ReceivedDate; a.IsActive = f.IsActive;
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { ModelState.AddModelError("", "তথ্য পরিবর্তিত হয়েছে অথবা কোডটি ব্যবহার হয়েছে। তালিকা থেকে আবার খুলুন।"); await Choices(); return View(f); }
        return RedirectToAction(nameof(Index));
    }
    private async Task Choices() => ViewBag.People = new SelectList(await db.Persons.OrderBy(x => x.NameBn).ToListAsync(), "Id", "NameBn");
}
