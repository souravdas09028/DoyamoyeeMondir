using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoyamoyeeMondir.Infrastructure
    .Persistence.Configurations;

public class PersonConfiguration
    : IEntityTypeConfiguration<Person>
{
    public void Configure(
        EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumber).HasMaxLength(50);
        builder.Property(x => x.SerialNumberKey).HasMaxLength(50)
            .HasComputedColumnSql("CASE WHEN NULLIF(LTRIM(RTRIM([SerialNumber])), N'') IS NULL THEN NULL ELSE RIGHT(REPLICATE(N'0', 50) + TRANSLATE(LTRIM(RTRIM([SerialNumber])), N'০১২৩৪৫৬৭৮৯', N'0123456789'), 50) END", stored: true);
        builder.HasIndex(x => x.SerialNumberKey).IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL");

        builder.Property(x => x.NameBn)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameEn)
            .HasMaxLength(200);

        builder.Property(x => x.MobileNumber)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.NationalIdNumber)
            .HasMaxLength(30);

        builder.Property(x => x.AddressBn)
            .HasMaxLength(1000);

        builder.Property(x => x.AddressEn)
            .HasMaxLength(1000);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}