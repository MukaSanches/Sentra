using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260920220000_InitialFoundation")]
public partial class InitialFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_events",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                entity_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                entity_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                outcome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                actor_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                correlation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_audit_events", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_correlation_id",
            table: "audit_events",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "ix_audit_events_occurred_at",
            table: "audit_events",
            column: "occurred_at");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "audit_events");
}
