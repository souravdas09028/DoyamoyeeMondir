using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Controllers;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
internal static class PrasadSqlChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        await using var seed=new ApplicationDbContext(options);
        var stock=new InventoryItem { NameBn="প্রসাদ পরীক্ষা", Unit="প্যাকেট" }; stock.Move(10);
        var product=new PrasadProduct { NameBn="প্রসাদ", InventoryItem=stock, Price=25 };
        var account=new CashBankAccount { AccountNameBn="প্রসাদ নগদ",AccountCode="PRASAD",OpeningBalanceDate=new DateOnly(2026,1,1) };
        var category=new IncomeCategory { NameBn="প্রসাদ আয়",Code="PRASAD" };
        seed.AddRange(product,account,category); await seed.SaveChangesAsync();
        var form=new PrasadSaleForm { ProductId=product.Id, ProductVersion=Convert.ToBase64String(product.RowVersion),Quantity=3,CashBankAccountId=account.Id,IncomeCategoryId=category.Id };
        await using(var db=new ApplicationDbContext(options)) Assert(await new PrasadController(db).Sell(form) is RedirectToActionResult,"Prasad sale returns income receipt");
        await using(var db=new ApplicationDbContext(options)) await new PrasadController(db).Sell(form);
        await using(var db=new ApplicationDbContext(options))
        {
            var sale=await db.PrasadSales.Include(x=>x.Income).SingleAsync(x=>x.PrasadProductId==product.Id);
            Assert(sale.Income.Amount==75 && sale.UnitPrice==25 && sale.Quantity==3,"Sale snapshots quantity and price");
            Assert((await db.InventoryItems.SingleAsync(x=>x.Id==stock.Id)).Quantity==7 && await db.StockMovements.CountAsync(x=>x.SubmissionKey==form.SubmissionKey)==1,"Duplicate sale reduces stock only once");
            Assert(await db.Incomes.CountAsync(x=>x.SubmissionKey==form.SubmissionKey)==1,"Sale creates exactly one income");
            var p=await db.PrasadProducts.SingleAsync(x=>x.Id==product.Id);
            var tooMuch=new PrasadSaleForm { ProductId=p.Id,ProductVersion=Convert.ToBase64String(p.RowVersion),Quantity=8,CashBankAccountId=account.Id,IncomeCategoryId=category.Id };
            var controller=new PrasadController(db);
            Assert(await controller.Sell(tooMuch) is ViewResult && !controller.ModelState.IsValid,"Insufficient prasad stock rejected");
            p.Price=30; await db.SaveChangesAsync();
            Assert(sale.UnitPrice==25 && sale.Income.Amount==75,"Price changes preserve prior receipts");
        }
        await using(var db=new ApplicationDbContext(options))
        {
            form.SubmissionKey=Guid.NewGuid(); var controller=new PrasadController(db);
            Assert(await controller.Sell(form) is ViewResult && !controller.ModelState.IsValid,"Stale prasad price rejected");
        }
        await using(var one=new ApplicationDbContext(options))
        await using(var two=new ApplicationDbContext(options))
        {
            var a=await one.InventoryItems.SingleAsync(x=>x.Id==stock.Id);
            var b=await two.InventoryItems.SingleAsync(x=>x.Id==stock.Id);
            a.Move(-5); await one.SaveChangesAsync();
            b.Move(-5);
            var income=new Income { Amount=150,Date=TempleDate.Today,IncomeCategoryId=category.Id,CashBankAccountId=account.Id,PayerName="test",CategoryName="test",AccountName="test",Description="race",SubmissionKey=Guid.NewGuid() };
            two.PrasadSales.Add(new PrasadSale { PrasadProductId=product.Id,Income=income,ProductName="race",Unit="packet",UnitPrice=30,Quantity=5 });
            var conflict=false; try { await two.SaveChangesAsync(); } catch(DbUpdateConcurrencyException) { conflict=true; }
            Assert(conflict,"Competing stock change rejects stale sale transaction");
        }
        await using(var db=new ApplicationDbContext(options)) Assert(!await db.Incomes.AnyAsync(x=>x.Description=="race") && await db.PrasadSales.CountAsync(x=>x.PrasadProductId==product.Id)==1,"Failed stock transaction rolls back sale and receipt");
    }
    private static void Assert(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); Console.WriteLine("PASS: "+message); }
}
