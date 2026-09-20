using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sentra.Infrastructure.Persistence;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
public sealed class SentraDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");

        modelBuilder.Entity("Sentra.Domain.Audit.AuditEvent", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<string>("Action").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
            b.Property<string>("CorrelationId").HasMaxLength(128).HasColumnType("character varying(128)");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<Guid?>("CreatedBy").HasColumnType("uuid");
            b.Property<string>("EntityId").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
            b.Property<string>("EntityType").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
            b.Property<string>("Result").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("CorrelationId");
            b.ToTable("audit_events");
        });

        modelBuilder.Entity("Sentra.Domain.Integrations.WebhookEvent", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<Guid?>("CreatedBy").HasColumnType("uuid");
            b.Property<string>("ExternalEventId").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            b.Property<string>("PayloadHash").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
            b.Property<DateTimeOffset?>("ProcessedAt").HasColumnType("timestamp with time zone");
            b.Property<string>("Provider").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<DateTimeOffset>("ReceivedAt").HasColumnType("timestamp with time zone");
            b.Property<string>("Status").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("Provider", "ExternalEventId").IsUnique();
            b.ToTable("webhook_events");
        });
    }
}
