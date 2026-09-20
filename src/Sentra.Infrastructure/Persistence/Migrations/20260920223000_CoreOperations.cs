using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260920223000_CoreOperations")]
public partial class CoreOperations : Migration
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
                code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_permissions", x => x.id));

        migrationBuilder.CreateTable(
            name: "residents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_residents", x => x.id));

        migrationBuilder.CreateTable(
            name: "blocks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blocks", x => x.id);
                table.ForeignKey(
                    name: "FK_blocks_condominiums_condominium_id",
                    column: x => x.condominium_id,
                    principalTable: "condominiums",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.id);
                table.ForeignKey(
                    name: "FK_roles_condominiums_condominium_id",
                    column: x => x.condominium_id,
                    principalTable: "condominiums",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "units",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                block_id = table.Column<Guid>(type: "uuid", nullable: true),
                identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                display_name = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_units", x => x.id);
                table.ForeignKey(
                    name: "FK_units_blocks_block_id",
                    column: x => x.block_id,
                    principalTable: "blocks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_units_condominiums_condominium_id",
                    column: x => x.condominium_id,
                    principalTable: "condominiums",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "resident_phones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                whatsapp_enabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_phones", x => x.id);
                table.ForeignKey(
                    name: "FK_resident_phones_residents_resident_id",
                    column: x => x.resident_id,
                    principalTable: "residents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_role_permissions",
                    x => new { x.role_id, x.permission_id });
                table.ForeignKey(
                    name: "FK_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                normalized_username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employees", x => x.id);
                table.ForeignKey(
                    name: "FK_employees_condominiums_condominium_id",
                    column: x => x.condominium_id,
                    principalTable: "condominiums",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_employees_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resident_units",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                is_primary = table.Column<bool>(type: "boolean", nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resident_units", x => x.id);
                table.ForeignKey(
                    name: "FK_resident_units_residents_resident_id",
                    column: x => x.resident_id,
                    principalTable: "residents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_resident_units_units_unit_id",
                    column: x => x.unit_id,
                    principalTable: "units",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_blocks_condominium_name",
            table: "blocks",
            columns: new[] { "condominium_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_employees_role_id",
            table: "employees",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "ux_employees_condominium_username",
            table: "employees",
            columns: new[] { "condominium_id", "normalized_username" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_permissions_code",
            table: "permissions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_resident_phones_e164",
            table: "resident_phones",
            column: "e164",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_resident_phones_resident_id",
            table: "resident_phones",
            column: "resident_id");

        migrationBuilder.CreateIndex(
            name: "IX_resident_units_unit_id",
            table: "resident_units",
            column: "unit_id");

        migrationBuilder.CreateIndex(
            name: "ux_resident_units_resident_unit",
            table: "resident_units",
            columns: new[] { "resident_id", "unit_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_permissions_permission_id",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ux_roles_condominium_name",
            table: "roles",
            columns: new[] { "condominium_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_units_block_id",
            table: "units",
            column: "block_id");

        migrationBuilder.CreateIndex(
            name: "ux_units_condominium_identifier",
            table: "units",
            columns: new[] { "condominium_id", "identifier" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "employees");
        migrationBuilder.DropTable(name: "resident_phones");
        migrationBuilder.DropTable(name: "resident_units");
        migrationBuilder.DropTable(name: "role_permissions");
        migrationBuilder.DropTable(name: "residents");
        migrationBuilder.DropTable(name: "units");
        migrationBuilder.DropTable(name: "permissions");
        migrationBuilder.DropTable(name: "roles");
        migrationBuilder.DropTable(name: "blocks");
        migrationBuilder.DropTable(name: "condominiums");
    }
}
