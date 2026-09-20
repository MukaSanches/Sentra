using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentra.Domain.Access;
using Sentra.Domain.Auditing;
using Sentra.Domain.Common;
using Sentra.Domain.Properties;
using Sentra.Domain.Residents;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
public partial class SentraDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");

        ConfigureAuditEvent(modelBuilder.Entity<AuditEvent>());
        ConfigureCondominium(modelBuilder.Entity<Condominium>());
        ConfigureBlock(modelBuilder.Entity<Block>());
        ConfigureUnit(modelBuilder.Entity<Unit>());
        ConfigureResident(modelBuilder.Entity<Resident>());
        ConfigureResidentPhone(modelBuilder.Entity<ResidentPhone>());
        ConfigureResidentUnit(modelBuilder.Entity<ResidentUnit>());
        ConfigureEmployee(modelBuilder.Entity<Employee>());
        ConfigureRole(modelBuilder.Entity<Role>());
        ConfigurePermission(modelBuilder.Entity<Permission>());
        ConfigureEmployeeRole(modelBuilder.Entity<EmployeeRole>());
        ConfigureRolePermission(modelBuilder.Entity<RolePermission>());
    }

    private static void ConfigureAuditEvent(EntityTypeBuilder<AuditEvent> entity)
    {
        entity.ToTable("audit_events");
        ConfigureBase(entity);
        entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(160).IsRequired();
        entity.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(160).IsRequired();
        entity.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(160).IsRequired();
        entity.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(80).IsRequired();
        entity.Property(x => x.ActorId).HasColumnName("actor_id").HasMaxLength(160);
        entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(160);
        entity.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_events_occurred_at");
        entity.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_events_correlation_id");
    }

    private static void ConfigureCondominium(EntityTypeBuilder<Condominium> entity)
    {
        entity.ToTable("condominiums");
        ConfigureAudited(entity);
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        entity.HasIndex(x => x.Name).HasDatabaseName("ix_condominiums_name");
    }

    private static void ConfigureBlock(EntityTypeBuilder<Block> entity)
    {
        entity.ToTable("blocks");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(40);
        entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        entity.HasIndex(x => new { x.CondominiumId, x.Name }).IsUnique().HasDatabaseName("ux_blocks_condominium_name");
        entity.HasIndex(x => new { x.CondominiumId, x.Code }).IsUnique().HasFilter(""" "code" IS NOT NULL """).HasDatabaseName("ux_blocks_condominium_code");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUnit(EntityTypeBuilder<Unit> entity)
    {
        entity.ToTable("units");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.BlockId).HasColumnName("block_id").IsRequired();
        entity.Property(x => x.Number).HasColumnName("number").HasMaxLength(40).IsRequired();
        entity.Property(x => x.Floor).HasColumnName("floor").HasMaxLength(40);
        entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        entity.HasIndex(x => new { x.BlockId, x.Number }).IsUnique().HasDatabaseName("ux_units_block_number");
        entity.HasIndex(x => x.CondominiumId).HasDatabaseName("ix_units_condominium");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Block>().WithMany().HasForeignKey(x => x.BlockId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureResident(EntityTypeBuilder<Resident> entity)
    {
        entity.ToTable("residents");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        entity.HasIndex(x => new { x.CondominiumId, x.FullName }).HasDatabaseName("ix_residents_condominium_name");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureResidentPhone(EntityTypeBuilder<ResidentPhone> entity)
    {
        entity.ToTable("resident_phones", table => table.HasCheckConstraint("ck_resident_phones_e164", """ "e164_number" ~ '^\+[1-9][0-9]{7,14}$' """));
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.ResidentId).HasColumnName("resident_id").IsRequired();
        entity.Property(x => x.E164Number).HasColumnName("e164_number").HasMaxLength(16).IsRequired();
        entity.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        entity.Property(x => x.IsWhatsAppEnabled).HasColumnName("is_whatsapp_enabled").IsRequired();
        entity.HasIndex(x => new { x.CondominiumId, x.E164Number }).IsUnique().HasDatabaseName("ux_resident_phones_condominium_e164");
        entity.HasIndex(x => x.ResidentId).HasFilter(""" "is_primary" = TRUE """).IsUnique().HasDatabaseName("ux_resident_phones_primary");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Resident>().WithMany().HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureResidentUnit(EntityTypeBuilder<ResidentUnit> entity)
    {
        entity.ToTable("resident_units", table => table.HasCheckConstraint("ck_resident_units_dates", """ "ends_at" IS NULL OR "ends_at" > "starts_at" """));
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.ResidentId).HasColumnName("resident_id").IsRequired();
        entity.Property(x => x.UnitId).HasColumnName("unit_id").IsRequired();
        entity.Property(x => x.Role).HasColumnName("role").HasConversion<int>().IsRequired();
        entity.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        entity.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        entity.Property(x => x.EndsAt).HasColumnName("ends_at");
        entity.HasIndex(x => new { x.ResidentId, x.UnitId }).IsUnique().HasFilter(""" "ends_at" IS NULL """).HasDatabaseName("ux_resident_units_active_link");
        entity.HasIndex(x => x.ResidentId).IsUnique().HasFilter(""" "is_primary" = TRUE AND "ends_at" IS NULL """).HasDatabaseName("ux_resident_units_primary");
        entity.HasIndex(x => x.CondominiumId).HasDatabaseName("ix_resident_units_condominium");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Resident>().WithMany().HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEmployee(EntityTypeBuilder<Employee> entity)
    {
        entity.ToTable("employees");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.IdentitySubject).HasColumnName("identity_subject").HasMaxLength(200).IsRequired();
        entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        entity.HasIndex(x => new { x.CondominiumId, x.IdentitySubject }).IsUnique().HasDatabaseName("ux_employees_condominium_subject");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRole(EntityTypeBuilder<Role> entity)
    {
        entity.ToTable("roles");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        entity.HasIndex(x => new { x.CondominiumId, x.Name }).IsUnique().HasDatabaseName("ux_roles_condominium_name");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePermission(EntityTypeBuilder<Permission> entity)
    {
        entity.ToTable("permissions");
        ConfigureBase(entity);
        entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(120).IsRequired();
        entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_permissions_code");
    }

    private static void ConfigureEmployeeRole(EntityTypeBuilder<EmployeeRole> entity)
    {
        entity.ToTable("employee_roles");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        entity.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        entity.HasIndex(x => new { x.EmployeeId, x.RoleId }).IsUnique().HasDatabaseName("ux_employee_roles_pair");
        entity.HasIndex(x => x.CondominiumId).HasDatabaseName("ix_employee_roles_condominium");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRolePermission(EntityTypeBuilder<RolePermission> entity)
    {
        entity.ToTable("role_permissions");
        ConfigureAudited(entity);
        entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
        entity.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        entity.Property(x => x.PermissionId).HasColumnName("permission_id").IsRequired();
        entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique().HasDatabaseName("ux_role_permissions_pair");
        entity.HasIndex(x => x.CondominiumId).HasDatabaseName("ix_role_permissions_condominium");
        entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBase<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : EntityBase
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }

    private static void ConfigureAudited<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : AuditedEntityBase
    {
        ConfigureBase(entity);
        entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(160).IsRequired();
        entity.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(160).IsRequired();
    }
}
