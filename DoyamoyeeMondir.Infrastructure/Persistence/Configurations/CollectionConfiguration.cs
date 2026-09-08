using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> b)
    {
        b.Property(x => x.TypeName).HasMaxLength(150).IsRequired();
        b.Property(x => x.AgreedAmount).HasPrecision(18, 2);
        b.Property(x => x.CollectedAmount).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.SubmissionKey).IsUnique();
        b.HasIndex(x => x.PreviousMembershipId).IsUnique().HasFilter("[PreviousMembershipId] IS NOT NULL");
        b.HasOne<Membership>().WithMany().HasForeignKey(x => x.PreviousMembershipId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PersonId, x.MembershipTypeId, x.StartDate }).IsUnique();
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MembershipType).WithMany().HasForeignKey(x => x.MembershipTypeId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("Memberships", t => {
            t.HasCheckConstraint("CK_Membership_Amounts", "[AgreedAmount] >= 0 AND [CollectedAmount] >= 0 AND [CollectedAmount] <= [AgreedAmount]");
            t.HasCheckConstraint("CK_Membership_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
        });
    }
}

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.TempleShare).HasPrecision(18, 2);
        b.Property(x => x.PriestShare).HasPrecision(18, 2);
        b.Property(x => x.StaffShare).HasPrecision(18, 2);
        b.Property(x => x.PayerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.CategoryName).HasMaxLength(150).IsRequired();
        b.Property(x => x.AccountName).HasMaxLength(150).IsRequired();
        b.Property(x => x.ServiceName).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.SubmissionKey).IsUnique();
        b.HasIndex(x => x.Date);
        b.HasOne(x => x.IncomeCategory).WithMany().HasForeignKey(x => x.IncomeCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Membership).WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TempleService).WithMany().HasForeignKey(x => x.TempleServiceId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("Incomes", t => {
            t.HasCheckConstraint("CK_Income_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Income_Source", "[MembershipId] IS NULL OR [TempleServiceId] IS NULL");
            t.HasCheckConstraint("CK_Income_Shares", "[TempleShare] >= 0 AND [PriestShare] >= 0 AND [StaffShare] >= 0 AND ([TempleServiceId] IS NULL OR [Amount] = [TempleShare] + [PriestShare] + [StaffShare])");
        });
    }
}
