using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppTenantConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_conversations_resident_id",
                table: "conversations",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_unit_id",
                table: "conversations",
                column: "unit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_condominiums_condominium_id",
                table: "conversations",
                column: "condominium_id",
                principalTable: "condominiums",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_residents_resident_id",
                table: "conversations",
                column: "resident_id",
                principalTable: "residents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_units_unit_id",
                table: "conversations",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_integrations_condominiums_condominium_id",
                table: "integrations",
                column: "condominium_id",
                principalTable: "condominiums",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_condominiums_condominium_id",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_conversations_residents_resident_id",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_conversations_units_unit_id",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_integrations_condominiums_condominium_id",
                table: "integrations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_resident_id",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_unit_id",
                table: "conversations");
        }
    }
}
