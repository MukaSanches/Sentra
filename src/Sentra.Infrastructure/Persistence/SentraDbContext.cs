using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Audit;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Integrations;
using Sentra.Domain.Residents;
using Sentra.Domain.Security;

namespace Sentra.Infrastructure.Persistence;

public sealed class SentraDbContext(DbContextOptions<SentraDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
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

        modelBuilder.Entity<Condominium>(entity =>
        {
            entity.ToTable("condominiums");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("blocks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.Name }).IsUnique();
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Identifier).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(96).IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.Identifier }).IsUnique();
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Block>().WithMany().HasForeignKey(x => x.BlockId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.ToTable("residents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<ResidentPhone>(entity =>
        {
            entity.ToTable("resident_phones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.E164).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.E164).IsUnique();
            entity.HasOne<Resident>().WithMany().HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResidentUnit>(entity =>
        {
            entity.ToTable("resident_units");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ResidentId, x.UnitId }).IsUnique();
            entity.HasOne<Resident>().WithMany().HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(96).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.HasIndex(x => new { x.CondominiumId, x.Name }).IsUnique();
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NormalizedUsername).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.NormalizedUsername }).IsUnique();
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
