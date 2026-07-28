using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoyamoyeeMondir.Infrastructure
    .Persistence.Configurations;

public class CashBankAccountConfiguration : IEntityTypeConfiguration<CashBankAccount>
{
    public void Configure(
        EntityTypeBuilder<CashBankAccount> builder)
    {
        builder.ToTable("CashBankAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNameBn)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.AccountNameEn)
            .HasMaxLength(150);

        builder.Property(x => x.AccountCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.AccountCode)
            .IsUnique();

        builder.Property(x => x.OpeningBalance)
            .HasPrecision(18, 2);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}