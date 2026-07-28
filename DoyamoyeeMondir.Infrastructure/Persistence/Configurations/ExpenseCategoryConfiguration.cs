using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoyamoyeeMondir.Infrastructure
    .Persistence.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(
        EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameBn)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NameEn)
            .HasMaxLength(150);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}