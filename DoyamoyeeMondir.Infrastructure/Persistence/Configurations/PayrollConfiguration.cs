using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        b.Property(x => x.Position).HasMaxLength(100).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(30);
        b.Property(x => x.MonthlySalary).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.ToTable("Employees", t => t.HasCheckConstraint("CK_Employee_Salary", "[MonthlySalary] >= 0"));
    }
}
public class PayrollConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> b)
    {
        b.Property(x => x.EmployeeName).HasMaxLength(150).IsRequired();
        b.Property(x => x.BaseSalary).HasPrecision(18, 2);
        b.Property(x => x.Allowance).HasPrecision(18, 2);
        b.Property(x => x.Deduction).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.EmployeeId, x.Month }).IsUnique();
        b.HasIndex(x => x.ExpenseId).IsUnique();
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Expense).WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("PayrollEntries", t => t.HasCheckConstraint("CK_Payroll_Amount", "DAY([Month]) = 1 AND [BaseSalary] >= 0 AND [Allowance] >= 0 AND [Deduction] >= 0 AND [BaseSalary] + [Allowance] > [Deduction]"));
    }
}
