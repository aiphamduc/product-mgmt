using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> entity)
    {
        entity.ToTable("ProductAttributeDefinition");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.ValueType).HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
        entity.HasOne(x => x.Category).WithMany(x => x.AttributeDefinitions).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> entity)
    {
        entity.ToTable("ProductAttributeValue");
        entity.HasKey(x => new { x.ProductId, x.AttributeDefinitionId });
        entity.Property(x => x.TextValue).HasMaxLength(4000);
        entity.Property(x => x.DecimalValue).HasPrecision(18, 4);
        entity.HasOne(x => x.Product).WithMany(x => x.AttributeValues).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.AttributeDefinition).WithMany(x => x.Values).HasForeignKey(x => x.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
