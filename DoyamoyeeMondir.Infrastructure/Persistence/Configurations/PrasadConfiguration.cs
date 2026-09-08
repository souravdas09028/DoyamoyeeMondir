using DoyamoyeeMondir.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DoyamoyeeMondir.Infrastructure.Persistence.Configurations;
public class PrasadProductConfiguration : IEntityTypeConfiguration<PrasadProduct>
{
    public void Configure(EntityTypeBuilder<PrasadProduct> b)
    {
        b.Property(x=>x.NameBn).HasMaxLength(150).IsRequired();
        b.Property(x=>x.Price).HasPrecision(18,2);
        b.Property(x=>x.RowVersion).IsRowVersion();
        b.HasOne(x=>x.InventoryItem).WithMany().HasForeignKey(x=>x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("PrasadProducts",t=>t.HasCheckConstraint("CK_Prasad_Price","[Price] > 0"));
    }
}
public class PrasadSaleConfiguration : IEntityTypeConfiguration<PrasadSale>
{
    public void Configure(EntityTypeBuilder<PrasadSale> b)
    {
        b.Property(x=>x.ProductName).HasMaxLength(150).IsRequired();
        b.Property(x=>x.Unit).HasMaxLength(30).IsRequired();
        b.Property(x=>x.UnitPrice).HasPrecision(18,2);
        b.Property(x=>x.RowVersion).IsRowVersion();
        b.HasIndex(x=>x.IncomeId).IsUnique();
        b.HasOne(x=>x.Income).WithMany().HasForeignKey(x=>x.IncomeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.PrasadProduct).WithMany().HasForeignKey(x=>x.PrasadProductId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable("PrasadSales",t=>t.HasCheckConstraint("CK_PrasadSale_Amount","[Quantity] > 0 AND [UnitPrice] > 0"));
    }
}
