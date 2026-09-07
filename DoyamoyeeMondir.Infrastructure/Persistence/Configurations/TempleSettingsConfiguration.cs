using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;

public class MembershipTypeConfiguration : IEntityTypeConfiguration<MembershipType>
{
    public void Configure(EntityTypeBuilder<MembershipType> builder)
    {
        builder.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.ToTable("MembershipTypes", t => t.HasCheckConstraint("CK_MembershipType_Amount", "[Amount] >= 0"));
    }
}

public class TempleServiceConfiguration : IEntityTypeConfiguration<TempleService>
{
    public void Configure(EntityTypeBuilder<TempleService> builder)
    {
        builder.Property(x => x.NameBn).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TempleShare).HasPrecision(18, 2);
        builder.Property(x => x.PriestShare).HasPrecision(18, 2);
        builder.Property(x => x.StaffShare).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.ToTable("TempleServices", t => t.HasCheckConstraint("CK_TempleService_Allocation",
            "[Amount] >= 0 AND [TempleShare] >= 0 AND [PriestShare] >= 0 AND [StaffShare] >= 0 AND [Amount] = [TempleShare] + [PriestShare] + [StaffShare]"));
    }
}
