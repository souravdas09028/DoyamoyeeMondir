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