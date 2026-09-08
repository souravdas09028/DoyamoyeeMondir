using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Controllers;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

internal static class OperationSqlChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        await using var seed = new ApplicationDbContext(options);
        var cash = new CashBankAccount { AccountCode = "OPS-CASH", AccountNameBn = "নগদ", OpeningBalance = 100, OpeningBalanceDate = new DateOnly(2026, 1, 1) };
        var bank = new CashBankAccount { AccountCode = "OPS-BANK", AccountNameBn = "ব্যাংক", OpeningBalanceDate = new DateOnly(2026, 1, 1) };
        var category = new ExpenseCategory { NameBn = "খরচ", Code = "OPS" };
        var item = new InventoryItem { NameBn = "চাল", Unit = "কেজি" };
        seed.AddRange(cash, bank, category, item); await seed.SaveChangesAsync();
        var expense = new Expense { ExpenseCategoryId = category.Id, Amount = 80, Date = new DateOnly(2026, 1, 1), Description = "test", SubmissionKey = Guid.NewGuid() }; expense.Submit(false);
        seed.Add(expense); await seed.SaveChangesAsync();
        var form = new PaymentForm { ExpenseId = expense.Id, CashBankAccountId = cash.Id, Amount = 40, Payee = "test" };
        await using (var db = new ApplicationDbContext(options)) Assert(await new AccountsController(db).Pay(form) is RedirectToActionResult, "SQL expense payment produces voucher");
        await using (var db = new ApplicationDbContext(options)) Assert(await new AccountsController(db).Pay(form) is RedirectToActionResult, "Repeated payment returns original voucher");
        await using (var db = new ApplicationDbContext(options)) Assert(await db.ExpensePayments.CountAsync(x => x.ExpenseId == expense.Id) == 1 && (await db.Expenses.SingleAsync(x => x.Id == expense.Id)).PaidAmount == 40, "Duplicate payment does not reduce balance twice");
        var transfer = new TransferForm { FromAccountId = cash.Id, ToAccountId = bank.Id, Amount = 20, Description = "transfer" };
        await using (var db = new ApplicationDbContext(options)) await new AccountsController(db).Transfer(transfer);
        await using (var db = new ApplicationDbContext(options)) await new AccountsController(db).Transfer(transfer);
        await using (var db = new ApplicationDbContext(options))
        {
            Assert(await db.AccountTransfers.CountAsync(x => x.SubmissionKey == transfer.SubmissionKey) == 1, "Duplicate transfer creates one movement");
            var result = (ViewResult)await new AccountsController(db).Index(null);
            var balances = (List<AccountBalance>)result.Model!;
            Assert(balances.Single(x => x.Id == cash.Id).Balance == 40 && balances.Single(x => x.Id == bank.Id).Balance == 20, "Balances count payment once and transfer on both sides");
        }
        await using (var one = new ApplicationDbContext(options))
        await using (var two = new ApplicationDbContext(options))
        {
            var e1 = await one.Expenses.SingleAsync(x => x.Id == expense.Id); var e2 = await two.Expenses.SingleAsync(x => x.Id == expense.Id);
            e1.Pay(30); one.ExpensePayments.Add(Payment(30)); await one.SaveChangesAsync();
            e2.Pay(30); two.ExpensePayments.Add(Payment(30));
            var conflict = false; try { await two.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflict = true; }
            Assert(conflict, "Concurrent payments cannot exceed approved expense");
        }
        var stock = new StockForm { InventoryItemId = item.Id, IsReceipt = true, Quantity = 10, Reason = "opening" };
        await using (var db = new ApplicationDbContext(options)) await new InventoryController(db).Move(stock);
        await using (var db = new ApplicationDbContext(options)) await new InventoryController(db).Move(stock);
        await using (var one = new ApplicationDbContext(options))
        await using (var two = new ApplicationDbContext(options))
        {
            var i1 = await one.InventoryItems.SingleAsync(x => x.Id == item.Id); var i2 = await two.InventoryItems.SingleAsync(x => x.Id == item.Id);
            Assert(i1.Quantity == 10, "Duplicate stock receipt does not increase quantity twice");
            i1.Move(-8); one.StockMovements.Add(Movement(-8)); await one.SaveChangesAsync();
            i2.Move(-8); two.StockMovements.Add(Movement(-8));
            var conflict = false; try { await two.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflict = true; }
            Assert(conflict, "Concurrent stock issues cannot overdraw inventory");
        }
        await using (var db = new ApplicationDbContext(options))
        {
            Assert((await db.InventoryItems.SingleAsync(x => x.Id == item.Id)).Quantity == 2 && await db.StockMovements.CountAsync(x => x.InventoryItemId == item.Id) == 2, "Failed stock issue rolls back quantity and movement");
            Assert((await db.Expenses.SingleAsync(x => x.Id == expense.Id)).PaidAmount == 70 && await db.ExpensePayments.CountAsync(x => x.ExpenseId == expense.Id) == 2, "Failed payment rolls back voucher and paid amount");
        }
        ExpensePayment Payment(decimal amount) => new() { ExpenseId = expense.Id, CashBankAccountId = cash.Id, AccountName = cash.AccountNameBn, Payee = "test", Amount = amount, Date = TempleDate.Today, SubmissionKey = Guid.NewGuid(), PaymentMethod = PaymentMethod.Cash };
        StockMovement Movement(decimal change) => new() { InventoryItemId = item.Id, Change = change, Date = TempleDate.Today, Reason = "use", SubmissionKey = Guid.NewGuid() };
    }
    private static void Assert(bool result, string message) { if (!result) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
