using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Auditing;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Residents;
using Sentra.Domain.Security;

namespace Sentra.Infrastructure.Persistence;

public sealed class SentraDbContext(DbContextOptions<SentraDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Condominium> Condominiums => Set<Condominium>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<ResidentPhone> ResidentPhones => Set<ResidentPhone>();
    public DbSet<ResidentUnit> ResidentUnits => Set<ResidentUnit>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Sentra.Domain.Integrations.Integration> Integrations => Set<Sentra.Domain.Integrations.Integration>();
    public DbSet<Sentra.Domain.Integrations.WebhookEvent> WebhookEvents => Set<Sentra.Domain.Integrations.WebhookEvent>();
    public DbSet<Sentra.Domain.Conversations.Conversation> Conversations => Set<Sentra.Domain.Conversations.Conversation>();
    public DbSet<Sentra.Domain.Conversations.ConversationParticipant> ConversationParticipants => Set<Sentra.Domain.Conversations.ConversationParticipant>();
    public DbSet<Sentra.Domain.Conversations.Message> Messages => Set<Sentra.Domain.Conversations.Message>();
    public DbSet<Sentra.Domain.Conversations.Attachment> Attachments => Set<Sentra.Domain.Conversations.Attachment>();
    public DbSet<Sentra.Domain.Conversations.MessageStatusEvent> MessageStatusEvents => Set<Sentra.Domain.Conversations.MessageStatusEvent>();

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

        modelBuilder.Entity<Condominium>(entity =>
        {
            entity.ToTable("condominiums");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("blocks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.Name })
                .IsUnique()
                .HasDatabaseName("ux_blocks_condominium_name");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.BlockId).HasColumnName("block_id");
            entity.Property(x => x.Identifier).HasColumnName("identifier").HasMaxLength(64).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(96).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.Identifier })
                .IsUnique()
                .HasDatabaseName("ux_units_condominium_identifier");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Block>()
                .WithMany()
                .HasForeignKey(x => x.BlockId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.ToTable("residents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        });

        modelBuilder.Entity<ResidentPhone>(entity =>
        {
            entity.ToTable("resident_phones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.ResidentId).HasColumnName("resident_id").IsRequired();
            entity.Property(x => x.E164).HasColumnName("e164").HasMaxLength(20).IsRequired();
            entity.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
            entity.Property(x => x.WhatsAppEnabled).HasColumnName("whatsapp_enabled").IsRequired();
            entity.HasIndex(x => x.E164).IsUnique().HasDatabaseName("ux_resident_phones_e164");
            entity.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(x => x.ResidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResidentUnit>(entity =>
        {
            entity.ToTable("resident_units");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.ResidentId).HasColumnName("resident_id").IsRequired();
            entity.Property(x => x.UnitId).HasColumnName("unit_id").IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
            entity.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
            entity.Property(x => x.EndsAt).HasColumnName("ends_at");
            entity.HasIndex(x => new { x.ResidentId, x.UnitId })
                .IsUnique()
                .HasDatabaseName("ux_resident_units_resident_unit");
            entity.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(x => x.ResidentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Unit>()
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(96).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_permissions_code");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
            entity.HasIndex(x => new { x.CondominiumId, x.Name })
                .IsUnique()
                .HasDatabaseName("ux_roles_condominium_name");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.Property(x => x.RoleId).HasColumnName("role_id");
            entity.Property(x => x.PermissionId).HasColumnName("permission_id");
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(80).IsRequired();
            entity.Property(x => x.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(x => x.FailedLoginAttempts).HasColumnName("failed_login_attempts").IsRequired();
            entity.Property(x => x.LockedUntil).HasColumnName("locked_until");
            entity.HasIndex(x => new { x.CondominiumId, x.NormalizedUsername })
                .IsUnique()
                .HasDatabaseName("ux_employees_condominium_username");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.ConfigureMessagingModel();
    }
}
