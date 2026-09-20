using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260920213000_InitialFoundation")]
public sealed class InitialFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_audit_events", x => x.Id));

        migrationBuilder.CreateTable(
            name: "webhook_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExternalEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_webhook_events", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_CorrelationId",
            table: "audit_events",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_webhook_events_Provider_ExternalEventId",
            table: "webhook_events",
            columns: new[] { "Provider", "ExternalEventId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_events");
        migrationBuilder.DropTable(name: "webhook_events");
    }
}
