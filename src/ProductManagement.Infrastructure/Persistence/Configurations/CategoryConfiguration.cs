using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public static readonly Guid GeneralCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("Category");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
        entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasData(new Category
        {
            Id = GeneralCategoryId,
            Name = "General",
            IsActive = true
        });
    }
}
