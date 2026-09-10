using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;
[Authorize(Roles="SuperAdmin,TempleAdmin")]
public class CorrectionsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page=1)
    {
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.HasNext=await db.FinancialReversals.CountAsync()>page*30;
        return View(await db.FinancialReversals.AsNoTracking().OrderByDescending(x=>x.Id).Skip((page-1)*30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Reverse(int kind,int id)
    {
        if(await db.FinancialReversals.AnyAsync(x=>x.SourceKind==kind && x.SourceId==id)) return RedirectToAction(nameof(Index));
        try { var s=await new FinancialCorrections(db).Source(kind,id); ViewBag.Source=s; return View(new ReversalForm { SourceKind=kind,SourceId=id,Version=s.Version }); }
        catch(InvalidOperationException) { return NotFound(); }
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Reverse(ReversalForm f)
    {
        if(ModelState.IsValid)
        {
            try { await new FinancialCorrections(db).Reverse(f); return RedirectToAction(nameof(Index)); }
            catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        }
        db.ChangeTracker.Clear();
        try { ViewBag.Source=await new FinancialCorrections(db).Source(f.SourceKind,f.SourceId); }
        catch(InvalidOperationException) { return NotFound(); }
        return View(f);
    }
}
