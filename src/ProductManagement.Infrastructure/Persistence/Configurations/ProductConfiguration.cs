using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Product");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(10000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(x => x.RowVersion).IsRowVersion();
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.HasIndex(x => new { x.Status, x.CategoryId, x.UpdatedUtc, x.Id });
        entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
