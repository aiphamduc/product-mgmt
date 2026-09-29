using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> entity)
    {
        entity.ToTable("StockAdjustment");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        entity.HasCheckConstraint("CK_StockAdjustment_Quantity_NonZero", "[Quantity] <> 0");
        entity.HasCheckConstraint("CK_StockAdjustment_Stocks_NonNegative", "[PreviousStock] >= 0 AND [NewStock] >= 0");
        entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
