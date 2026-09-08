using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DoyamoyeeMondir.Web.Controllers;
[Authorize(Roles="SuperAdmin,TempleAdmin,Accountant,Treasurer,DonationCollector")]
public class PrasadController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.PrasadProducts.AsNoTracking().Include(x=>x.InventoryItem).OrderBy(x=>x.NameBn).ToListAsync());
    [Authorize(Roles="SuperAdmin,TempleAdmin")]
    public async Task<IActionResult> Edit(int id=0)
    {
        await Items();
        if(id==0) return View(new PrasadProductForm());
        var p=await db.PrasadProducts.FindAsync(id);
        return p is null ? NotFound() : View(new PrasadProductForm { Id=p.Id, NameBn=p.NameBn, InventoryItemId=p.InventoryItemId, Price=p.Price, IsActive=p.IsActive, RowVersion=Convert.ToBase64String(p.RowVersion) });
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="SuperAdmin,TempleAdmin")]
    public async Task<IActionResult> Edit(PrasadProductForm f)
    {
        await Items();
        var p=f.Id==0 ? new PrasadProduct() : await db.PrasadProducts.FindAsync(f.Id);
        if(p is null) return NotFound();
        if(!await db.InventoryItems.AnyAsync(x=>x.Id==f.InventoryItemId && x.IsActive)) ModelState.AddModelError("","সক্রিয় মজুত পণ্য নির্বাচন করুন।");
        if(f.Id!=0 && p.InventoryItemId!=f.InventoryItemId && await db.PrasadSales.AnyAsync(x=>x.PrasadProductId==p.Id)) ModelState.AddModelError("","বিক্রয়ের পরে সংযুক্ত মজুত পণ্য পরিবর্তন করা যাবে না।");
        if(!ModelState.IsValid) return View(f);
        if(f.Id==0) db.Add(p);
        else { try { db.Entry(p).Property(x=>x.RowVersion).OriginalValue=Convert.FromBase64String(f.RowVersion??""); } catch(FormatException) { return BadRequest(); } }
        p.NameBn=f.NameBn.Trim(); p.InventoryItemId=f.InventoryItemId; p.Price=f.Price; p.IsActive=f.IsActive;
        try { await db.SaveChangesAsync(); } catch(DbUpdateConcurrencyException) { return Conflict("তথ্য পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।"); }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Sell(int id)
    {
        var p=await db.PrasadProducts.AsNoTracking().Include(x=>x.InventoryItem).SingleOrDefaultAsync(x=>x.Id==id && x.IsActive && x.InventoryItem.IsActive);
        if(p is null) return NotFound();
        await Choices(p); return View(new PrasadSaleForm { ProductId=id,ProductVersion=Convert.ToBase64String(p.RowVersion) });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Sell(PrasadSaleForm f)
    {
        var prior=await db.PrasadSales.AsNoTracking().Include(x=>x.Income).SingleOrDefaultAsync(x=>x.Income.SubmissionKey==f.SubmissionKey);
        if(prior!=null) return RedirectToAction("Receipt","Income",new { id=prior.IncomeId });
        var p=await db.PrasadProducts.Include(x=>x.InventoryItem).SingleOrDefaultAsync(x=>x.Id==f.ProductId && x.IsActive && x.InventoryItem.IsActive);
        if(p is null) return NotFound();
        var account=await db.CashBankAccounts.SingleOrDefaultAsync(x=>x.Id==f.CashBankAccountId && x.IsActive);
        var category=await db.IncomeCategories.SingleOrDefaultAsync(x=>x.Id==f.IncomeCategoryId && x.IsActive);
        if(account is null || !account.CanPost(f.Date)) ModelState.AddModelError("","সক্রিয় হিসাব ও হিসাবের বৈধ তারিখ নির্বাচন করুন।");
        if(category is null) ModelState.AddModelError("","সক্রিয় আয়ের ধরন নির্বাচন করুন।");
        if(f.ProductVersion!=Convert.ToBase64String(p.RowVersion)) ModelState.AddModelError("","পণ্যের দাম পরিবর্তিত হয়েছে। ফর্মটি আবার খুলুন।");
        if(f.Quantity>p.InventoryItem.Quantity) ModelState.AddModelError("","পর্যাপ্ত মজুত নেই।");
        if(await db.StockMovements.Where(x=>x.InventoryItemId==p.InventoryItemId).MaxAsync(x=>(DateOnly?)x.Date)>f.Date) ModelState.AddModelError("","সর্বশেষ মজুত লেনদেনের আগের তারিখ ব্যবহার করা যাবে না।");
        if(!ModelState.IsValid) { await Choices(p); return View(f); }
        var income=new Income { Date=f.Date, Amount=p.Price*f.Quantity, IncomeCategoryId=category!.Id, CategoryName=category.NameBn, CashBankAccountId=account!.Id, AccountName=account.AccountNameBn, PayerName=string.IsNullOrWhiteSpace(f.PayerName)?"সাধারণ ভক্ত":f.PayerName.Trim(), Description=$"প্রসাদ: {p.NameBn} — {f.Quantity} {p.InventoryItem.Unit} × {p.Price:0.00}", PaymentMethod=f.PaymentMethod, Reference=f.Reference?.Trim(), SubmissionKey=f.SubmissionKey };
        db.PrasadSales.Add(new PrasadSale { PrasadProduct=p, Income=income, ProductName=p.NameBn, Unit=p.InventoryItem.Unit, UnitPrice=p.Price, Quantity=f.Quantity });
        p.InventoryItem.Move(-f.Quantity);
        db.StockMovements.Add(new StockMovement { InventoryItemId=p.InventoryItemId, Date=f.Date, Change=-f.Quantity, Reason=$"প্রসাদ বিক্রয়: {p.NameBn}", SubmissionKey=f.SubmissionKey });
        db.Entry(p).Property(x=>x.IsActive).IsModified=true;
        db.Entry(account).Property(x=>x.IsActive).IsModified=true;
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException)
        {
            var saved=await db.PrasadSales.AsNoTracking().SingleOrDefaultAsync(x=>x.Income.SubmissionKey==f.SubmissionKey);
            if(saved!=null) return RedirectToAction("Receipt","Income",new { id=saved.IncomeId });
            return Conflict("মজুত বা হিসাব পরিবর্তিত হয়েছে। পণ্য তালিকা থেকে আবার খুলুন।");
        }
        return RedirectToAction("Receipt","Income",new { id=income.Id });
    }
    public async Task<IActionResult> Sales(int page=1)
    {
        page=Math.Clamp(page,1,1000000); ViewBag.Page=page; ViewBag.HasNext=await db.PrasadSales.CountAsync()>page*30;
        return View(await db.PrasadSales.AsNoTracking().Include(x=>x.Income).OrderByDescending(x=>x.Id).Skip((page-1)*30).Take(30).ToListAsync());
    }
    private async Task Items() => ViewBag.Items=new SelectList(await db.InventoryItems.AsNoTracking().OrderBy(x=>x.NameBn).ToListAsync(),"Id","NameBn");
    private async Task Choices(PrasadProduct p)
    {
        ViewBag.Product=p;
        ViewBag.Categories=new SelectList(await db.IncomeCategories.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.NameBn).ToListAsync(),"Id","NameBn");
        ViewBag.Accounts=new SelectList(await db.CashBankAccounts.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.AccountNameBn).ToListAsync(),"Id","AccountNameBn");
    }
}
