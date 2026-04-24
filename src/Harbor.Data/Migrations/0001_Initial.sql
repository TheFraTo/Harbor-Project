-- ==============================================================================
-- Migration 0001 — Initial schema Harbor
--
-- Conforme à harbor-architecture.md §9.2, enrichi avec :
--   - table __harbor_migrations pour suivre les migrations appliquées
--   - colonne priority dans transfers (Transfer record a ce champ)
--   - colonne description dans snippets (Snippet record a ce champ)
--   - index supplémentaires sur workspace_id, status, timestamp
--   - FK explicites avec ON DELETE SET NULL pour préserver l'historique
-- ==============================================================================

-- ------------------------------------------------------------------------------
-- Table de suivi des migrations appliquées
-- ------------------------------------------------------------------------------
CREATE TABLE __harbor_migrations (
    version     INTEGER PRIMARY KEY,
    name        TEXT NOT NULL,
    applied_at  INTEGER NOT NULL
);

-- ------------------------------------------------------------------------------
-- Workspaces — regroupements de profils (projet / client)
-- ------------------------------------------------------------------------------
CREATE TABLE workspaces (
    id          TEXT PRIMARY KEY,
    name        TEXT NOT NULL,
    icon        TEXT,
    color       TEXT,
    notes       TEXT,
    created_at  INTEGER NOT NULL,
    updated_at  INTEGER NOT NULL
);

-- ------------------------------------------------------------------------------
-- Profils de connexion
-- ------------------------------------------------------------------------------
CREATE TABLE profiles (
    id                  TEXT PRIMARY KEY,
    name                TEXT NOT NULL,
    protocol            TEXT NOT NULL,
    workspace_id        TEXT REFERENCES workspaces(id) ON DELETE SET NULL,
    parent_folder_id    TEXT,
    connection_json     TEXT NOT NULL,
    auth_json           TEXT NOT NULL,
    tags                TEXT,           -- CSV (ex : "prod,web,hetzner")
    env_vars_json       TEXT,
    post_connect_script TEXT,
    notes               TEXT,
    created_at          INTEGER NOT NULL,
    updated_at          INTEGER NOT NULL,
    last_used_at        INTEGER
);

-- ------------------------------------------------------------------------------
-- Clés SSH gérées dans le keystore
-- La partie privée est chiffrée AES-256-GCM : 3 colonnes (nonce, ciphertext, tag).
-- ------------------------------------------------------------------------------
CREATE TABLE ssh_keys (
    id                      TEXT PRIMARY KEY,
    name                    TEXT NOT NULL,
    algorithm               TEXT NOT NULL,
    private_key_nonce       BLOB NOT NULL,
    private_key_ciphertext  BLOB NOT NULL,
    private_key_tag         BLOB NOT NULL,
    public_key              BLOB NOT NULL,
    comment                 TEXT,
    created_at              INTEGER NOT NULL,
    last_used_at            INTEGER
);

-- ------------------------------------------------------------------------------
-- Transferts (file d'attente persistante)
-- ------------------------------------------------------------------------------
CREATE TABLE transfers (
    id                  TEXT PRIMARY KEY,
    direction           TEXT NOT NULL,
    source_path         TEXT NOT NULL,
    dest_path           TEXT NOT NULL,
    source_profile_id   TEXT REFERENCES profiles(id) ON DELETE SET NULL,
    dest_profile_id     TEXT REFERENCES profiles(id) ON DELETE SET NULL,
    total_bytes         INTEGER NOT NULL,
    transferred_bytes   INTEGER NOT NULL,
    status              TEXT NOT NULL,
    error_message       TEXT,
    priority            INTEGER NOT NULL DEFAULT 0,
    created_at          INTEGER NOT NULL,
    completed_at        INTEGER
);

-- ------------------------------------------------------------------------------
-- Journal d'audit (append-only)
-- ------------------------------------------------------------------------------
CREATE TABLE audit_log (
    id              TEXT PRIMARY KEY,
    timestamp       INTEGER NOT NULL,
    type            TEXT NOT NULL,
    profile_id      TEXT REFERENCES profiles(id) ON DELETE SET NULL,
    description     TEXT NOT NULL,
    metadata_json   TEXT
);

-- ------------------------------------------------------------------------------
-- Snippets de commandes (avec variables éventuelles)
-- ------------------------------------------------------------------------------
CREATE TABLE snippets (
    id              TEXT PRIMARY KEY,
    name            TEXT NOT NULL,
    description     TEXT,
    command         TEXT NOT NULL,
    variables_json  TEXT,
    tags            TEXT,
    created_at      INTEGER NOT NULL
);

-- ==============================================================================
-- Index
-- ==============================================================================
CREATE INDEX idx_profiles_name          ON profiles(name);
CREATE INDEX idx_profiles_workspace     ON profiles(workspace_id);
CREATE INDEX idx_profiles_tags          ON profiles(tags);
CREATE INDEX idx_profiles_last_used     ON profiles(last_used_at DESC);
CREATE INDEX idx_transfers_status       ON transfers(status);
CREATE INDEX idx_transfers_created      ON transfers(created_at DESC);
CREATE INDEX idx_transfers_priority     ON transfers(priority DESC, created_at ASC);
CREATE INDEX idx_audit_log_timestamp    ON audit_log(timestamp DESC);
CREATE INDEX idx_audit_log_type         ON audit_log(type);
CREATE INDEX idx_audit_log_profile      ON audit_log(profile_id);
CREATE INDEX idx_snippets_name          ON snippets(name);
CREATE INDEX idx_ssh_keys_name          ON ssh_keys(name);
