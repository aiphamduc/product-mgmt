using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        entity.ToTable("ProductVariant");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Price).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.Sku).IsUnique();
        entity.HasCheckConstraint("CK_ProductVariant_Price_NonNegative", "[Price] >= 0");
        entity.HasCheckConstraint("CK_ProductVariant_Stock_NonNegative", "[StockOnHand] >= 0");
        entity.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
