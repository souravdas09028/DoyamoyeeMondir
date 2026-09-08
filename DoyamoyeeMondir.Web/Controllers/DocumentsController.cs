using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using DoyamoyeeMondir.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;

[Authorize(Roles = "SuperAdmin,TempleAdmin,DocumentManager")]
public class DocumentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        page = Math.Clamp(page, 1, 1000000);
        var query = db.TempleDocuments.AsNoTracking().Where(x => q == null || x.Title.Contains(q) || x.Reference != null && x.Reference.Contains(q));
        ViewBag.Q = q; ViewBag.Page = page; ViewBag.HasNext = await query.CountAsync() > page * 30;
        return View(await query.OrderByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).Select(x => new TempleDocument { Id = x.Id, Title = x.Title, Date = x.Date, Reference = x.Reference, FileName = x.FileName }).ToListAsync());
    }
    public IActionResult Upload() => View(new DocumentForm());
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload(DocumentForm f)
    {
        if (f.Date == default) ModelState.AddModelError("", "নথির তারিখ লিখুন।");
        if (f.Upload is null || f.Upload.Length == 0 || f.Upload.Length > DocumentFiles.MaxBytes) ModelState.AddModelError("", "সর্বোচ্চ ১০ মেগাবাইটের PDF, PNG বা JPEG দিন।");
        if (!ModelState.IsValid) return View(f);
        using var stream = new MemoryStream(); await f.Upload!.CopyToAsync(stream);
        var bytes = stream.ToArray(); var format = DocumentFiles.Detect(bytes);
        if (format is null) { ModelState.AddModelError("", "ফাইলটি সমর্থিত PDF, PNG বা JPEG নয়।"); return View(f); }
        var name = Path.GetFileNameWithoutExtension(f.Upload.FileName).Replace('\r', ' ').Replace('\n', ' ');
        if (name.Length > 200) name = name[..200]; if (string.IsNullOrWhiteSpace(name)) name = "document";
        db.TempleDocuments.Add(new TempleDocument { Title = f.Title.Trim(), Reference = f.Reference?.Trim(), Date = f.Date, FileName = name + format.Value.Extension, ContentType = format.Value.ContentType, Content = bytes });
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Download(int id)
    {
        var document = await db.TempleDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (document is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(document.Content, document.ContentType, document.FileName);
    }
}
