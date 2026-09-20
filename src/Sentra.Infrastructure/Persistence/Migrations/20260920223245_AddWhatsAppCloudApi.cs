using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentra.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppCloudApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "whatsapp_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_participant_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    resident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    last_inbound_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_conversations_condominiums_condominium_id",
                        column: x => x.condominium_id,
                        principalTable: "condominiums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsapp_conversations_residents_resident_id",
                        column: x => x.resident_id,
                        principalTable: "residents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waba_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_phone_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    verified_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    quality_rating = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_app_subscribed = table.Column<bool>(type: "boolean", nullable: false),
                    last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_webhook_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_integrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_integrations_condominiums_condominium_id",
                        column: x => x.condominium_id,
                        principalTable: "condominiums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    message_type = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    reply_to_external_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    raw_payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    message_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivery_status = table.Column<int>(type: "integer", nullable: false),
                    delivery_status_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    error_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_messages_condominiums_condominium_id",
                        column: x => x.condominium_id,
                        principalTable: "condominiums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsapp_messages_whatsapp_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "whatsapp_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condominium_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    content = table.Column<byte[]>(type: "bytea", nullable: true),
                    downloaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_attachments_condominiums_condominium_id",
                        column: x => x.condominium_id,
                        principalTable: "condominiums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsapp_attachments_whatsapp_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "whatsapp_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_attachments_condominium_id",
                table: "whatsapp_attachments",
                column: "condominium_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_attachments_media",
                table: "whatsapp_attachments",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_attachments_message",
                table: "whatsapp_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_conversations_recent",
                table: "whatsapp_conversations",
                columns: new[] { "condominium_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_conversations_resident_id",
                table: "whatsapp_conversations",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "ux_whatsapp_conversations_participant",
                table: "whatsapp_conversations",
                columns: new[] { "condominium_id", "external_participant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_whatsapp_integrations_condominium",
                table: "whatsapp_integrations",
                column: "condominium_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_whatsapp_integrations_phone",
                table: "whatsapp_integrations",
                column: "phone_number_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_messages_condominium_id",
                table: "whatsapp_messages",
                column: "condominium_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_messages_conversation_time",
                table: "whatsapp_messages",
                columns: new[] { "conversation_id", "message_timestamp" });

            migrationBuilder.CreateIndex(
                name: "ux_whatsapp_messages_external",
                table: "whatsapp_messages",
                column: "external_message_id",
                unique: true,
                filter: " \"external_message_id\" IS NOT NULL ");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_webhook_events_queue",
                table: "whatsapp_webhook_events",
                columns: new[] { "processing_status", "next_attempt_at", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ux_whatsapp_webhook_events_hash",
                table: "whatsapp_webhook_events",
                column: "event_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_attachments");

            migrationBuilder.DropTable(
                name: "whatsapp_integrations");

            migrationBuilder.DropTable(
                name: "whatsapp_webhook_events");

            migrationBuilder.DropTable(
                name: "whatsapp_messages");

            migrationBuilder.DropTable(
                name: "whatsapp_conversations");
        }
    }
}
