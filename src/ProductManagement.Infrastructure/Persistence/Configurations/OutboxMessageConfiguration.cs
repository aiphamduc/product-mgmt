using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Core.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("OutboxMessage");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Type).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Payload).IsRequired();
        entity.HasIndex(x => new { x.ProcessedUtc, x.CreatedUtc });
    }
}
