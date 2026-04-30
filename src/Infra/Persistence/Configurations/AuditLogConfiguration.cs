using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id).HasColumnName("id");
        builder.Property(auditLog => auditLog.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(auditLog => auditLog.UserId).HasColumnName("user_id");
        builder.Property(auditLog => auditLog.UserName).HasColumnName("user_name").HasMaxLength(256);
        builder.Property(auditLog => auditLog.RoleNames).HasColumnName("role_names").HasMaxLength(512).IsRequired();
        builder.Property(auditLog => auditLog.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.EntityName).HasColumnName("entity_name").HasMaxLength(200).IsRequired();
        builder.Property(auditLog => auditLog.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(auditLog => auditLog.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(auditLog => auditLog.RequestPath).HasColumnName("request_path").HasMaxLength(2048);
        builder.Property(auditLog => auditLog.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
        builder.Property(auditLog => auditLog.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");

        builder.HasIndex(auditLog => auditLog.OccurredAt);
        builder.HasIndex(auditLog => auditLog.CorrelationId);
        builder.HasIndex(auditLog => new { auditLog.EntityName, auditLog.EntityId });
    }
}
