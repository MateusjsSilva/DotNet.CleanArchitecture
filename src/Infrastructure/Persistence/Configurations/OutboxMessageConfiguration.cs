using CleanArchitecture.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Content)
            .IsRequired();

        builder.HasIndex(m => m.ProcessedAt)
            .HasFilter("[ProcessedAt] IS NULL");
    }
}
