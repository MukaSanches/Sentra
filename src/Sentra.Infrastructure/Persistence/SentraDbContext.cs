using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Auditing;

namespace Sentra.Infrastructure.Persistence;

public sealed class SentraDbContext(DbContextOptions<SentraDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(160).IsRequired();
            entity.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(80).IsRequired();
            entity.Property(x => x.ActorId).HasColumnName("actor_id").HasMaxLength(160);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(160);
            entity.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_events_occurred_at");
            entity.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_events_correlation_id");
        });
    }
}
