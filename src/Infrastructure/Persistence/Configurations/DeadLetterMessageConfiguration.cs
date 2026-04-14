using CleanArchitecture.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

internal sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.Error)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.EventVersion)
            .HasDefaultValue(1);

        builder.Property(m => m.RetryCount)
            .HasDefaultValue(0);

        // Foreign key to OutboxMessage
        builder.HasOne(d => d.OutboxMessage)
            .WithOne()
            .HasForeignKey<DeadLetterMessage>(d => d.OutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index to find failed messages
        builder.HasIndex(m => m.FailedAt);

        // Index for reprocessing
        builder.HasIndex(m => m.ReprocessedAt)
            .HasFilter("[ReprocessedAt] IS NULL");
    }
}
