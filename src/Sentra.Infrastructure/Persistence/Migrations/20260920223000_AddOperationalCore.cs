using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260920223000_AddOperationalCore")]
public partial class AddOperationalCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "condominiums",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_condominiums", x => x.id));

        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_permissions", x => x.id));

        migrationBuilder.CreateTable(
            name: "blocks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blocks", x => x.id);
                table.ForeignKey("FK_blocks_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                identity_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employees", x => x.id);
                table.ForeignKey("FK_employees_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "residents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_residents", x => x.id);
                table.ForeignKey("FK_residents_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_system = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.id);
                table.ForeignKey("FK_roles_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "units",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                block_id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                floor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_units", x => x.id);
                table.ForeignKey("FK_units_blocks_block_id", x => x.block_id, "blocks", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_units_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "employee_roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_roles", x => x.id);
                table.ForeignKey("FK_employee_roles_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_employee_roles_employees_employee_id", x => x.employee_id, "employees", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_employee_roles_roles_role_id", x => x.role_id, "roles", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => x.id);
                table.ForeignKey("FK_role_permissions_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_role_permissions_permissions_permission_id", x => x.permission_id, "permissions", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_role_permissions_roles_role_id", x => x.role_id, "roles", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resident_phones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                e164_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                is_whatsapp_enabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_phones", x => x.id);
                table.CheckConstraint("ck_resident_phones_e164", """ "e164_number" ~ '^\+[1-9][0-9]{7,14}$' """);
                table.ForeignKey("FK_resident_phones_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_resident_phones_residents_resident_id", x => x.resident_id, "residents", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resident_units",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<int>(type: "integer", nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_units", x => x.id);
                table.CheckConstraint("ck_resident_units_dates", """ "ends_at" IS NULL OR "ends_at" > "starts_at" """);
                table.ForeignKey("FK_resident_units_condominiums_condominium_id", x => x.condominium_id, "condominiums", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_resident_units_residents_resident_id", x => x.resident_id, "residents", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_resident_units_units_unit_id", x => x.unit_id, "units", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("ix_condominiums_name", "condominiums", "name");
        migrationBuilder.CreateIndex("ux_permissions_code", "permissions", "code", unique: true);
        migrationBuilder.CreateIndex("ux_blocks_condominium_name", "blocks", new[] { "condominium_id", "name" }, unique: true);
        migrationBuilder.CreateIndex("ux_blocks_condominium_code", "blocks", new[] { "condominium_id", "code" }, unique: true, filter: """ "code" IS NOT NULL """);
        migrationBuilder.CreateIndex("ux_employees_condominium_subject", "employees", new[] { "condominium_id", "identity_subject" }, unique: true);
        migrationBuilder.CreateIndex("ix_residents_condominium_name", "residents", new[] { "condominium_id", "full_name" });
        migrationBuilder.CreateIndex("ux_roles_condominium_name", "roles", new[] { "condominium_id", "name" }, unique: true);
        migrationBuilder.CreateIndex("ix_units_condominium", "units", "condominium_id");
        migrationBuilder.CreateIndex("ux_units_block_number", "units", new[] { "block_id", "number" }, unique: true);
        migrationBuilder.CreateIndex("ix_employee_roles_condominium", "employee_roles", "condominium_id");
        migrationBuilder.CreateIndex("IX_employee_roles_role_id", "employee_roles", "role_id");
        migrationBuilder.CreateIndex("ux_employee_roles_pair", "employee_roles", new[] { "employee_id", "role_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_role_permissions_condominium", "role_permissions", "condominium_id");
        migrationBuilder.CreateIndex("IX_role_permissions_permission_id", "role_permissions", "permission_id");
        migrationBuilder.CreateIndex("ux_role_permissions_pair", "role_permissions", new[] { "role_id", "permission_id" }, unique: true);
        migrationBuilder.CreateIndex("IX_resident_phones_condominium_id", "resident_phones", "condominium_id");
        migrationBuilder.CreateIndex("ux_resident_phones_primary", "resident_phones", "resident_id", unique: true, filter: """ "is_primary" = TRUE """);
        migrationBuilder.CreateIndex("ux_resident_phones_condominium_e164", "resident_phones", new[] { "condominium_id", "e164_number" }, unique: true);
        migrationBuilder.CreateIndex("ix_resident_units_condominium", "resident_units", "condominium_id");
        migrationBuilder.CreateIndex("IX_resident_units_unit_id", "resident_units", "unit_id");
        migrationBuilder.CreateIndex("ux_resident_units_active_link", "resident_units", new[] { "resident_id", "unit_id" }, unique: true, filter: """ "ends_at" IS NULL """);
        migrationBuilder.CreateIndex("ux_resident_units_primary", "resident_units", "resident_id", unique: true, filter: """ "is_primary" = TRUE AND "ends_at" IS NULL """);

        migrationBuilder.Sql(
            """
            INSERT INTO permissions (id, created_at, updated_at, code, description) VALUES
            ('10000000-0000-0000-0000-000000000001', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'condominium.read', 'Consultar configuração do condomínio'),
            ('10000000-0000-0000-0000-000000000002', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'condominium.write', 'Alterar configuração do condomínio'),
            ('10000000-0000-0000-0000-000000000003', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'residents.read', 'Consultar moradores e unidades'),
            ('10000000-0000-0000-0000-000000000004', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'residents.write', 'Cadastrar e alterar moradores e vínculos'),
            ('10000000-0000-0000-0000-000000000005', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'employees.read', 'Consultar funcionários'),
            ('10000000-0000-0000-0000-000000000006', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'employees.manage', 'Gerenciar funcionários'),
            ('10000000-0000-0000-0000-000000000007', TIMESTAMPTZ '2026-09-20 00:00:00+00', TIMESTAMPTZ '2026-09-20 00:00:00+00', 'roles.manage', 'Gerenciar papéis e permissões');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("employee_roles");
        migrationBuilder.DropTable("resident_phones");
        migrationBuilder.DropTable("resident_units");
        migrationBuilder.DropTable("role_permissions");
        migrationBuilder.DropTable("employees");
        migrationBuilder.DropTable("residents");
        migrationBuilder.DropTable("units");
        migrationBuilder.DropTable("permissions");
        migrationBuilder.DropTable("roles");
        migrationBuilder.DropTable("blocks");
        migrationBuilder.DropTable("condominiums");
    }
}
