using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Audit;
using Sentra.Domain.Integrations;

namespace Sentra.Infrastructure.Persistence;

public sealed class SentraDbContext(DbContextOptions<SentraDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Result).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.ToTable("webhook_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExternalEventId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
        });
    }
}
