using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class DomainEventHandlerExecutionConfiguration : IEntityTypeConfiguration<DomainEventHandlerExecutionEntity>
{
    public void Configure(EntityTypeBuilder<DomainEventHandlerExecutionEntity> builder)
    {
        builder.ToTable("domain_event_handler_executions");

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.Id).HasColumnName("id");
        builder.Property(execution => execution.DomainEventId).HasColumnName("domain_event_id").IsRequired();
        builder.Property(execution => execution.EventName).HasColumnName("event_name").HasMaxLength(200).IsRequired();
        builder.Property(execution => execution.HandlerName).HasColumnName("handler_name").HasMaxLength(500).IsRequired();
        builder.Property(execution => execution.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(execution => execution.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(execution => execution.LastError).HasColumnName("last_error");
        builder.Property(execution => execution.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(execution => execution.StartedAt).HasColumnName("started_at");
        builder.Property(execution => execution.CompletedAt).HasColumnName("completed_at");

        builder.HasOne(execution => execution.DomainEvent)
            .WithMany(domainEvent => domainEvent.HandlerExecutions)
            .HasForeignKey(execution => execution.DomainEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(execution => execution.DomainEventId);
        builder.HasIndex(execution => execution.Status);
    }
}
