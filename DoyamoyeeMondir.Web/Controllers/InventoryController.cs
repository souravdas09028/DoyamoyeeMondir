using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,InventoryManager")]
public class InventoryController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.InventoryItems.AsNoTracking().Where(x => q == null || x.NameBn.Contains(q));
        ViewBag.Q = q; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > page * 30;
        return View(await query.OrderBy(x => x.NameBn).ThenBy(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new InventoryForm());
        var item = await db.InventoryItems.FindAsync(id); if (item is null) return NotFound();
        return View(new InventoryForm { Id = id, NameBn = item.NameBn, Unit = item.Unit, IsActive = item.IsActive, ReorderLevel = item.ReorderLevel, RowVersion = Convert.ToBase64String(item.RowVersion) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var item = form.Id == 0 ? new InventoryItem() : await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == form.Id);
        if (item is null) return NotFound();
        if (form.Id == 0) db.Add(item);
        else
        {
            if (item.Unit != form.Unit.Trim() && await db.StockMovements.AnyAsync(x => x.InventoryItemId == item.Id)) { ModelState.AddModelError("", "মজুত লেনদেনের পরে একক পরিবর্তন করা যাবে না।"); return View(form); }
            try { db.Entry(item).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(form.RowVersion ?? ""); } catch (FormatException) { return BadRequest(); }
        }
        item.NameBn = form.NameBn.Trim(); item.Unit = form.Unit.Trim(); item.ReorderLevel = form.ReorderLevel; item.IsActive = form.IsActive;
        try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "তথ্য পরিবর্তিত হয়েছে। তালিকা থেকে আবার খুলুন।"); return View(form); }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Move(int id, bool receipt = true)
    {
        var item = await db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive); if (item is null) return NotFound();
        ViewBag.Item = item; return View(new StockForm { InventoryItemId = id, IsReceipt = receipt });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(StockForm form)
    {
        if (await db.StockMovements.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(History), new { id = form.InventoryItemId });
        var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == form.InventoryItemId && x.IsActive); if (item is null) return NotFound();
        ViewBag.Item = item;
        // Preserve chronological stock history: backdating before the latest movement is not allowed.
        var lastDate = await db.StockMovements.Where(x => x.InventoryItemId == item.Id).MaxAsync(x => (DateOnly?)x.Date);
        if (lastDate > form.Date) ModelState.AddModelError("", "সর্বশেষ মজুত লেনদেনের আগের তারিখ ব্যবহার করা যাবে না।");
        if (!form.IsReceipt && form.Quantity > item.Quantity) ModelState.AddModelError("", "পর্যাপ্ত মজুত নেই।");
        if (!ModelState.IsValid) return View(form);
        var change = form.IsReceipt ? form.Quantity : -form.Quantity; item.Move(change);
        db.StockMovements.Add(new StockMovement { InventoryItemId = item.Id, Change = change, Date = form.Date, Reason = form.Reason.Trim(), SubmissionKey = form.SubmissionKey });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.StockMovements.AnyAsync(x => x.SubmissionKey == form.SubmissionKey)) return RedirectToAction(nameof(History), new { id = item.Id });
            ModelState.AddModelError("", "মজুত পরিবর্তিত হয়েছে বা সংরক্ষণ করা যায়নি। তালিকা থেকে আবার খুলুন।"); return View(form);
        }
        return RedirectToAction(nameof(History), new { id = item.Id });
    }
    public async Task<IActionResult> History(int id, int page = 1)
    {
        var item = await db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (item is null) return NotFound();
        page = Math.Clamp(page, 1, 1000000); ViewBag.Item = item; ViewBag.Page = page;
        var query = db.StockMovements.AsNoTracking().Where(x => x.InventoryItemId == id); ViewBag.HasNext = await query.CountAsync() > page * 30;
        return View(await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
}
