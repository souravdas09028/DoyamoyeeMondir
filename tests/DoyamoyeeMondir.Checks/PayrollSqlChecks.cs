using DoyamoyeeMondir.Domain.Entities;
using DoyamoyeeMondir.Domain.Enums;
using DoyamoyeeMondir.Infrastructure.Persistence;
using DoyamoyeeMondir.Web.Controllers;
using DoyamoyeeMondir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

internal static class PayrollSqlChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        await using var seed=new ApplicationDbContext(options);
        var employee=new Employee { NameBn="বেতন পরীক্ষা", Position="কর্মী", MonthlySalary=100, JoinedOn=new DateOnly(2026,1,1) };
        var category=new ExpenseCategory { NameBn="বেতন", Code="PAYROLL", RequiresApproval=true };
        seed.AddRange(employee,category); await seed.SaveChangesAsync();
        var form=new PayrollForm { EmployeeId=employee.Id, EmployeeVersion=Convert.ToBase64String(employee.RowVersion), ExpenseCategoryId=category.Id, Allowance=20, Deduction=5 };
        await using(var db=new ApplicationDbContext(options)) Assert(await new EmployeesController(db).Generate(form) is RedirectToActionResult,"Payroll creates linked expense");
        await using(var db=new ApplicationDbContext(options)) Assert(await new EmployeesController(db).Generate(form) is RedirectToActionResult,"Payroll duplicate submission is idempotent");
        await using(var db=new ApplicationDbContext(options))
        {
            var payroll=await db.PayrollEntries.Include(x=>x.Expense).SingleAsync(x=>x.EmployeeId==employee.Id);
            Assert(payroll.Expense.Amount==115 && payroll.Expense.Status==ApprovalStatus.Submitted && payroll.Expense.PaidAmount==0,"Payroll snapshots amount and requires approval without paying");
            Assert(await db.Expenses.CountAsync(x=>x.SubmissionKey==form.SubmissionKey)==1,"Payroll creates exactly one expense");
            var current=await db.Employees.SingleAsync(x=>x.Id==employee.Id);
            var duplicate=new PayrollForm { EmployeeId=employee.Id, EmployeeVersion=Convert.ToBase64String(current.RowVersion), ExpenseCategoryId=category.Id };
            var controller=new EmployeesController(db);
            Assert(await controller.Generate(duplicate) is ViewResult && !controller.ModelState.IsValid,"Second payroll for same employee and month is rejected");
        }
        await using(var db=new ApplicationDbContext(options))
        {
            var e=await db.Employees.SingleAsync(x=>x.Id==employee.Id); e.MonthlySalary=200; await db.SaveChangesAsync();
            var payroll=await db.PayrollEntries.SingleAsync(x=>x.EmployeeId==employee.Id);
            Assert(payroll.BaseSalary==100,"Salary changes preserve historical payroll");
        }
        await using(var db=new ApplicationDbContext(options))
        {
            var stale=new PayrollForm { EmployeeId=employee.Id, EmployeeVersion=form.EmployeeVersion, ExpenseCategoryId=category.Id, Month=form.Month.AddMonths(-1) };
            var controller=new EmployeesController(db);
            Assert(await controller.Generate(stale) is ViewResult && !controller.ModelState.IsValid,"Stale salary form is rejected");
        }
        await using(var db=new ApplicationDbContext(options))
        {
            var duplicate=new PayrollEntry { EmployeeId=employee.Id, Month=form.Month, EmployeeName="duplicate", BaseSalary=100,
                Expense=new Expense { ExpenseCategoryId=category.Id, Date=form.Date, Amount=100, Description="duplicate", SubmissionKey=Guid.NewGuid() } };
            duplicate.Expense.Submit(false); db.PayrollEntries.Add(duplicate);
            var rejected=false; try { await db.SaveChangesAsync(); } catch(DbUpdateException) { rejected=true; }
            Assert(rejected,"Database unique index rejects competing payroll insert");
        }
        await using(var db=new ApplicationDbContext(options)) Assert(!await db.Expenses.AnyAsync(x=>x.Description=="duplicate"),"Rejected payroll rolls back linked expense");
    }
    private static void Assert(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); Console.WriteLine("PASS: "+message); }
}
