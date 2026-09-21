using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sentra.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SentraDbContext))]
[Migration("20260921010000_V1Operations")]
public sealed class V1Operations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS visitor_authorizations (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    unit_id uuid NOT NULL REFERENCES units(id) ON DELETE RESTRICT,
    resident_id uuid NULL REFERENCES residents(id) ON DELETE SET NULL,
    visitor_name varchar(160) NOT NULL,
    document varchar(64) NULL,
    relationship varchar(64) NULL,
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    vehicle_plate varchar(16) NULL,
    purpose varchar(256) NULL,
    status varchar(32) NOT NULL,
    checked_in_at timestamptz NULL,
    checked_out_at timestamptz NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_visitor_authorizations_condominium_window
    ON visitor_authorizations(condominium_id, starts_at, ends_at);
CREATE INDEX IF NOT EXISTS ix_visitor_authorizations_unit
    ON visitor_authorizations(unit_id);

CREATE TABLE IF NOT EXISTS visits (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    authorization_id uuid NOT NULL REFERENCES visitor_authorizations(id) ON DELETE CASCADE,
    action varchar(16) NOT NULL,
    occurred_at timestamptz NOT NULL,
    actor_id uuid NULL REFERENCES employees(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_visits_authorization ON visits(authorization_id, occurred_at);

CREATE TABLE IF NOT EXISTS qr_credentials (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    authorization_id uuid NOT NULL REFERENCES visitor_authorizations(id) ON DELETE CASCADE,
    token_hash varchar(128) NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    one_time boolean NOT NULL,
    used_at timestamptz NULL,
    revoked_at timestamptz NULL,
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS service_providers (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    name varchar(160) NOT NULL,
    document varchar(64) NULL,
    company varchar(160) NULL,
    active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_service_providers_condominium_name
    ON service_providers(condominium_id, name);

CREATE TABLE IF NOT EXISTS provider_authorizations (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    provider_id uuid NOT NULL REFERENCES service_providers(id) ON DELETE CASCADE,
    unit_id uuid NULL REFERENCES units(id) ON DELETE SET NULL,
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    purpose varchar(256) NOT NULL,
    status varchar(32) NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_provider_authorizations_window
    ON provider_authorizations(condominium_id, starts_at, ends_at);

CREATE TABLE IF NOT EXISTS packages (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    unit_id uuid NOT NULL REFERENCES units(id) ON DELETE RESTRICT,
    carrier varchar(120) NOT NULL,
    description varchar(512) NULL,
    received_at timestamptz NOT NULL,
    received_by uuid NULL REFERENCES employees(id) ON DELETE SET NULL,
    status varchar(32) NOT NULL,
    pickup_code_hash varchar(128) NULL,
    picked_up_at timestamptz NULL,
    picked_up_by uuid NULL REFERENCES employees(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_packages_condominium_status
    ON packages(condominium_id, status, received_at DESC);

CREATE TABLE IF NOT EXISTS occurrences (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    category varchar(64) NOT NULL,
    title varchar(180) NOT NULL,
    description text NOT NULL,
    priority varchar(24) NOT NULL,
    status varchar(32) NOT NULL,
    unit_id uuid NULL REFERENCES units(id) ON DELETE SET NULL,
    owner_employee_id uuid NULL REFERENCES employees(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_occurrences_condominium_status
    ON occurrences(condominium_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS shifts (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    opened_at timestamptz NOT NULL,
    closed_at timestamptz NULL,
    handoff_summary text NULL,
    acknowledged_by uuid NULL REFERENCES employees(id) ON DELETE SET NULL,
    acknowledged_at timestamptz NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_shifts_condominium_opened
    ON shifts(condominium_id, opened_at DESC);

CREATE TABLE IF NOT EXISTS announcements (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    title varchar(180) NOT NULL,
    body text NOT NULL,
    audience varchar(80) NOT NULL,
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NULL,
    active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_announcements_condominium_active
    ON announcements(condominium_id, active, starts_at DESC);

CREATE TABLE IF NOT EXISTS ai_interpretations (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    conversation_id uuid NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    intent varchar(80) NOT NULL,
    resolution_state varchar(48) NOT NULL,
    summary varchar(1000) NOT NULL,
    extracted_json jsonb NOT NULL,
    missing_json jsonb NOT NULL,
    created_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_ai_interpretations_conversation
    ON ai_interpretations(conversation_id, created_at DESC);

CREATE TABLE IF NOT EXISTS pending_actions (
    id uuid PRIMARY KEY,
    condominium_id uuid NOT NULL REFERENCES condominiums(id) ON DELETE CASCADE,
    conversation_id uuid NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    interpretation_id uuid NOT NULL REFERENCES ai_interpretations(id) ON DELETE CASCADE,
    action_type varchar(96) NOT NULL,
    risk_level varchar(48) NOT NULL,
    payload_json jsonb NOT NULL,
    status varchar(32) NOT NULL,
    created_at timestamptz NOT NULL,
    resolved_at timestamptz NULL,
    resolved_by uuid NULL REFERENCES employees(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_pending_actions_condominium_status
    ON pending_actions(condominium_id, status, created_at DESC);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS pending_actions;
DROP TABLE IF EXISTS ai_interpretations;
DROP TABLE IF EXISTS announcements;
DROP TABLE IF EXISTS shifts;
DROP TABLE IF EXISTS occurrences;
DROP TABLE IF EXISTS packages;
DROP TABLE IF EXISTS provider_authorizations;
DROP TABLE IF EXISTS service_providers;
DROP TABLE IF EXISTS qr_credentials;
DROP TABLE IF EXISTS visits;
DROP TABLE IF EXISTS visitor_authorizations;
""");
    }
}
