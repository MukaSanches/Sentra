using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260920220000_CoreOperations")]
public sealed class CoreOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "condominiums",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_condominiums", x => x.Id));

        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_permissions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "residents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_residents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "blocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blocks", x => x.Id);
                table.ForeignKey("FK_blocks_condominiums_CondominiumId", x => x.CondominiumId, "condominiums", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.Id);
                table.ForeignKey("FK_roles_condominiums_CondominiumId", x => x.CondominiumId, "condominiums", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "units",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_units", x => x.Id);
                table.ForeignKey("FK_units_blocks_BlockId", x => x.BlockId, "blocks", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_units_condominiums_CondominiumId", x => x.CondominiumId, "condominiums", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "resident_phones",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                E164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_phones", x => x.Id);
                table.ForeignKey("FK_resident_phones_residents_ResidentId", x => x.ResidentId, "residents", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey("FK_role_permissions_permissions_PermissionId", x => x.PermissionId, "permissions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_role_permissions_roles_RoleId", x => x.RoleId, "roles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                NormalizedUsername = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employees", x => x.Id);
                table.ForeignKey("FK_employees_condominiums_CondominiumId", x => x.CondominiumId, "condominiums", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_employees_roles_RoleId", x => x.RoleId, "roles", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resident_units",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_units", x => x.Id);
                table.ForeignKey("FK_resident_units_residents_ResidentId", x => x.ResidentId, "residents", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_resident_units_units_UnitId", x => x.UnitId, "units", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_blocks_CondominiumId_Name", "blocks", new[] { "CondominiumId", "Name" }, unique: true);
        migrationBuilder.CreateIndex("IX_employees_CondominiumId_NormalizedUsername", "employees", new[] { "CondominiumId", "NormalizedUsername" }, unique: true);
        migrationBuilder.CreateIndex("IX_employees_RoleId", "employees", "RoleId");
        migrationBuilder.CreateIndex("IX_permissions_Code", "permissions", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_resident_phones_E164", "resident_phones", "E164", unique: true);
        migrationBuilder.CreateIndex("IX_resident_phones_ResidentId", "resident_phones", "ResidentId");
        migrationBuilder.CreateIndex("IX_resident_units_ResidentId_UnitId", "resident_units", new[] { "ResidentId", "UnitId" }, unique: true);
        migrationBuilder.CreateIndex("IX_resident_units_UnitId", "resident_units", "UnitId");
        migrationBuilder.CreateIndex("IX_role_permissions_PermissionId", "role_permissions", "PermissionId");
        migrationBuilder.CreateIndex("IX_roles_CondominiumId_Name", "roles", new[] { "CondominiumId", "Name" }, unique: true);
        migrationBuilder.CreateIndex("IX_units_BlockId", "units", "BlockId");
        migrationBuilder.CreateIndex("IX_units_CondominiumId_Identifier", "units", new[] { "CondominiumId", "Identifier" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("employees");
        migrationBuilder.DropTable("resident_phones");
        migrationBuilder.DropTable("resident_units");
        migrationBuilder.DropTable("role_permissions");
        migrationBuilder.DropTable("residents");
        migrationBuilder.DropTable("units");
        migrationBuilder.DropTable("permissions");
        migrationBuilder.DropTable("roles");
        migrationBuilder.DropTable("blocks");
        migrationBuilder.DropTable("condominiums");
    }
}
