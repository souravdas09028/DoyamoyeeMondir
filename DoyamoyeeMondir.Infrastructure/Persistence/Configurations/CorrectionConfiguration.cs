using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;
public class ReversalConfiguration : IEntityTypeConfiguration<FinancialReversal>
{
    public void Configure(EntityTypeBuilder<FinancialReversal> b)
    {
        b.Property(x=>x.RowVersion).IsRowVersion(); b.Property(x=>x.Reason).HasMaxLength(1000).IsRequired(); b.Property(x=>x.CategoryName).HasMaxLength(150).IsRequired();
        foreach(var p in new[]{"Amount","IncomeDelta","ExpenseDelta","PriestDelta","StaffDelta"}) b.Property<decimal>(p).HasPrecision(18,2);
        b.HasIndex(x=>new{x.SourceKind,x.SourceId}).IsUnique(); b.HasIndex(x=>x.SubmissionKey).IsUnique();
        b.HasOne(x=>x.DebitAccount).WithMany().HasForeignKey(x=>x.DebitAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.CreditAccount).WithMany().HasForeignKey(x=>x.CreditAccountId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("FinancialReversals",t=>t.HasCheckConstraint("CK_Reversal_Source","[SourceKind] BETWEEN 1 AND 4 AND [SourceId] > 0 AND [Amount] > 0"));
    }
}
public class ReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> b)
    {
        b.Property(x=>x.RowVersion).IsRowVersion(); b.Property(x=>x.Reference).HasMaxLength(200).IsRequired();
        b.Property(x=>x.StatementBalance).HasPrecision(18,2); b.Property(x=>x.BookBalance).HasPrecision(18,2);
        b.HasIndex(x=>x.SubmissionKey).IsUnique();
        b.HasOne(x=>x.CashBankAccount).WithMany().HasForeignKey(x=>x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("BankReconciliations",t=>t.HasCheckConstraint("CK_Reconciliation_Closed","[IsClosed] = 0 OR [StatementBalance] = [BookBalance]"));
    }
}
