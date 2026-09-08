using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<ExpensePayment>
{
    public void Configure(EntityTypeBuilder<ExpensePayment> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.AccountName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Payee).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.SubmissionKey).IsUnique();
        b.HasOne(x => x.Expense).WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("ExpensePayments", t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
    }
}
public class TransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.SubmissionKey).IsUnique();
        b.HasOne(x => x.FromAccount).WithMany().HasForeignKey(x => x.FromAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToAccount).WithMany().HasForeignKey(x => x.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("AccountTransfers", t => t.HasCheckConstraint("CK_Transfer_Valid", "[Amount] > 0 AND [FromAccountId] <> [ToAccountId]"));
    }
}
public class InventoryConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 3);
        b.Property(x => x.ReorderLevel).HasPrecision(18, 3);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.ToTable("InventoryItems", t => t.HasCheckConstraint("CK_Inventory_Quantity", "[Quantity] >= 0 AND [ReorderLevel] >= 0"));
    }
}
public class StockConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.Property(x => x.Change).HasPrecision(18, 3);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.SubmissionKey).IsUnique();
        b.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("StockMovements", t => t.HasCheckConstraint("CK_Stock_Change", "[Change] <> 0"));
    }
}
public class AssetConfiguration : IEntityTypeConfiguration<TempleAsset>
{
    public void Configure(EntityTypeBuilder<TempleAsset> b)
    {
        b.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Location).HasMaxLength(200).IsRequired();
        b.Property(x => x.Custodian).HasMaxLength(200).IsRequired();
        b.Property(x => x.Material).HasMaxLength(100);
        b.Property(x => x.WeightGrams).HasPrecision(18, 3);
        b.Property(x => x.EstimatedValue).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Donor).WithMany().HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("TempleAssets", t => t.HasCheckConstraint("CK_Asset_Values", "([WeightGrams] IS NULL OR [WeightGrams] >= 0) AND ([EstimatedValue] IS NULL OR [EstimatedValue] >= 0)"));
    }
}
public class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> b)
    {
        b.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.ToTable("Committees", t => t.HasCheckConstraint("CK_Committee_Dates", "[EndDate] >= [StartDate]"));
    }
}
public class CommitteeMemberConfiguration : IEntityTypeConfiguration<CommitteeMember>
{
    public void Configure(EntityTypeBuilder<CommitteeMember> b)
    {
        b.Property(x => x.Position).HasMaxLength(100).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.CommitteeId, x.PersonId }).IsUnique();
        b.HasOne(x => x.Committee).WithMany().HasForeignKey(x => x.CommitteeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class DocumentConfiguration : IEntityTypeConfiguration<TempleDocument>
{
    public void Configure(EntityTypeBuilder<TempleDocument> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
