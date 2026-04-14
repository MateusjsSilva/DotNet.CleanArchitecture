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

        builder.Property(m => m.EventVersion)
            .HasDefaultValue(1);

        builder.Property(m => m.RetryCount)
            .HasDefaultValue(0);

        builder.Property(m => m.IdempotencyKey)
            .HasMaxLength(256);

        // Index to quickly find unprocessed messages
        builder.HasIndex(m => m.ProcessedAt)
            .HasFilter("[ProcessedAt] IS NULL");

        // Index for idempotency checking
        builder.HasIndex(m => m.IdempotencyKey)
            .HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}
