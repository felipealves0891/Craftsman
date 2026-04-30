using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class PersistedDomainEventConfiguration : IEntityTypeConfiguration<PersistedDomainEventEntity>
{
    public void Configure(EntityTypeBuilder<PersistedDomainEventEntity> builder)
    {
        builder.ToTable("domain_events");

        builder.HasKey(domainEvent => domainEvent.Id);

        builder.Property(domainEvent => domainEvent.Id)
            .HasColumnName("id");

        builder.Property(domainEvent => domainEvent.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(domainEvent => domainEvent.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(domainEvent => domainEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(domainEvent => domainEvent.UserId)
            .HasColumnName("user_id");

        builder.Property(domainEvent => domainEvent.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(256);

        builder.Property(domainEvent => domainEvent.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);
    }
}
