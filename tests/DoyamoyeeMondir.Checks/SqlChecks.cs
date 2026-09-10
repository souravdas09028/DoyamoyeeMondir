using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Controllers;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

internal static class SqlChecks
{
    public static async Task Run()
    {
        var databaseName = "DoyamoyeeChecks_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEMPLE_TEST_SQL") ?? "Server=.;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5") { InitialCatalog = databaseName };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var setup = new ApplicationDbContext(options);
        try
        {
            await setup.Database.MigrateAsync();
            var person = new Person { NameBn = "পরীক্ষার দাতা" };
            var type = new MembershipType { NameBn = "পরীক্ষার সদস্য", Amount = 100 };
            var category = new IncomeCategory { NameBn = "পরীক্ষার আয়", Code = "TEST" };
            var account = new CashBankAccount { AccountNameBn = "পরীক্ষার নগদ", AccountCode = "TEST", IsCashAccount = true, OpeningBalanceDate = new DateOnly(2026, 1, 1) };
            setup.AddRange(person, type, category, account); await setup.SaveChangesAsync();
            var membership = new Membership { PersonId = person.Id, StartDate = new DateOnly(2026, 1, 1), SubmissionKey = Guid.NewGuid() }; membership.SetTerms(type);
            setup.Add(membership); await setup.SaveChangesAsync();

            await using var first = new ApplicationDbContext(options);
            await using var second = new ApplicationDbContext(options);
            var m1 = await first.Memberships.SingleAsync();
            var m2 = await second.Memberships.SingleAsync();
            m1.Collect(30); first.Incomes.Add(NewIncome(30)); await first.SaveChangesAsync();
            m2.Collect(80); second.Incomes.Add(NewIncome(80));
            var conflict = false;
            try { await second.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflict = true; }
            Assert(conflict, "SQL detects competing membership collections");
            await using var verify = new ApplicationDbContext(options);
            Assert(await verify.Incomes.CountAsync() == 1 && (await verify.Memberships.SingleAsync()).CollectedAmount == 30, "Failed collection rolls back both receipt and membership balance");

            var key = Guid.NewGuid();
            var form = new IncomeForm { IncomeCategoryId = category.Id, CashBankAccountId = account.Id, MembershipId = membership.Id, Amount = 70, Date = new DateOnly(2026, 9, 8), Description = "চাঁদা", SubmissionKey = key };
            await using (var db = new ApplicationDbContext(options))
                Assert(await new IncomeController(db).Create(form) is RedirectToActionResult, "Membership collection returns a receipt");
            await using (var db = new ApplicationDbContext(options))
                Assert(await new IncomeController(db).Create(form) is RedirectToActionResult, "Repeated submission returns existing receipt");
            await using (var db = new ApplicationDbContext(options))
            {
                Assert(await db.Incomes.CountAsync(x => x.SubmissionKey == key) == 1 && (await db.Memberships.SingleAsync()).Outstanding == 0, "Duplicate POST does not duplicate receipt or collection");
            }

            var service = new TempleService { NameBn = "সেবা", Amount = 100, TempleShare = 60, PriestShare = 30, StaffShare = 10 };
            setup.TempleServices.Add(service); await setup.SaveChangesAsync();
            var version = Convert.ToBase64String(service.RowVersion);
            service.Amount = 200; service.TempleShare = 160; await setup.SaveChangesAsync();
            await using (var db = new ApplicationDbContext(options))
            {
                var controller = new IncomeController(db);
                var result = await controller.Create(new IncomeForm { IncomeCategoryId = category.Id, CashBankAccountId = account.Id, TempleServiceId = service.Id, ServiceVersion = version, Amount = 100, Description = "stale" });
                Assert(result is ViewResult && !controller.ModelState.IsValid, "Changed service pricing requires reopening form");
            }
            await using (var db = new ApplicationDbContext(options))
            {
                var controller = new IncomeController(db);
                var result = await controller.Create(new IncomeForm { IncomeCategoryId = category.Id, CashBankAccountId = account.Id, Date = new DateOnly(2025, 12, 31), Amount = 10, Description = "early" });
                Assert(result is ViewResult && !controller.ModelState.IsValid, "Collection cannot precede account opening date");
            }
            await OperationSqlChecks.Run(options);
            await PayrollSqlChecks.Run(options);
            await PrasadSqlChecks.Run(options);
            await CorrectionSqlChecks.Run(options);
            Console.WriteLine("PASS: SQL migration and integration checks");

            Income NewIncome(decimal amount) => new() { Amount = amount, MembershipId = membership.Id, PersonId = person.Id, IncomeCategoryId = category.Id, CashBankAccountId = account.Id,
                PayerName = person.NameBn, CategoryName = category.NameBn, AccountName = account.AccountNameBn, Date = new DateOnly(2026, 9, 8), Description = "test", PaymentMethod = PaymentMethod.Cash, SubmissionKey = Guid.NewGuid() };
        }
        finally
        {
            // Only the fresh, uniquely named database created by this run can be removed.
            if (connection.InitialCatalog == databaseName && databaseName.StartsWith("DoyamoyeeChecks_", StringComparison.Ordinal))
                await setup.Database.EnsureDeletedAsync();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS: " + message);
    }
}
