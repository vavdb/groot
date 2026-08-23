-- Groot device store, version 1.
-- Applied by GrootDatabase when user_version is below 1. Never edited after release:
-- a schema change is a new schema.vN.sql, so an existing database migrates forward.
--
-- Conventions, from Plan/sqlite-store-implementation.md:
--   * GUIDs and dates are TEXT. Dates are yyyy-MM-dd, which sorts correctly.
--   * Decimals are TEXT. SQLite REAL would turn 42.5 kg into 42.499999999999996.
--   * Instants are INTEGER unix milliseconds.
--   * updated_at / device_id / deleted appear only on tables that sync. Children of a synced
--     aggregate (sets, plates) travel with their parent and carry none of it.

CREATE TABLE users (
    id         TEXT NOT NULL PRIMARY KEY,
    -- NOCASE because Vincent and vincent are one person. Adding the collation later would be a
    -- table rebuild plus a de-duplication pass over accounts that should never have both existed.
    username   TEXT NOT NULL UNIQUE COLLATE NOCASE,
    created_at INTEGER NOT NULL
) STRICT;

-- Synced aggregate root. A session is created, performed and finished on one device, so the
-- whole document is the sync unit: last writer wins, no per-field merge.
CREATE TABLE sessions (
    id            TEXT NOT NULL PRIMARY KEY,
    user_id       TEXT NOT NULL REFERENCES users(id),
    date          TEXT NOT NULL,
    kind          TEXT NOT NULL CHECK (kind IN ('lift', 'run', 'rest_claim')),
    program_id    TEXT,
    day_key       TEXT,
    interval_week INTEGER,
    interval_day  INTEGER,
    duration_s    INTEGER,
    notes         TEXT,
    updated_at    INTEGER NOT NULL,
    device_id     TEXT NOT NULL,
    deleted       INTEGER NOT NULL DEFAULT 0 CHECK (deleted IN (0, 1)),

    -- date(date) returns NULL for anything that is not a canonical yyyy-MM-dd, and IS compares
    -- NULL properly where = would not: 'bogus' = NULL is NULL, which passes a CHECK.
    CHECK (date IS date(date)),

    -- Each kind needs different columns, and nothing else enforces it. A lift with no day_key
    -- stores, renders and credits the contract, then falls out of progression without a sound.
    CHECK (
        (kind = 'lift' AND program_id IS NOT NULL AND day_key IS NOT NULL)
        OR (kind = 'run' AND program_id IS NOT NULL AND interval_week IS NOT NULL AND interval_day IS NOT NULL)
        OR (kind = 'rest_claim' AND program_id IS NULL)
    )
) STRICT;

CREATE INDEX ix_sessions_user_date ON sessions (user_id, date);
CREATE INDEX ix_sessions_user_program_date ON sessions (user_id, program_id, date);

-- Child of sessions: deleted and re-inserted whenever its session is saved. A removed set is a
-- set that is no longer in the list, which is why there is no tombstone here.
CREATE TABLE sets (
    id           TEXT NOT NULL PRIMARY KEY,
    session_id   TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    exercise_id  TEXT NOT NULL,
    set_order    INTEGER NOT NULL,
    weight_kg    TEXT NOT NULL,
    reps         INTEGER,
    entry_mode   TEXT NOT NULL CHECK (entry_mode IN ('PerSide', 'Total', 'PerHand')),
    entry_weight TEXT NOT NULL,
    entry_unit   TEXT NOT NULL CHECK (entry_unit IN ('Kg', 'Lb')),

    -- A soft reference, deliberately not a foreign key. Equipment is a different sync aggregate,
    -- so a session pulled before its equipment would be rejected on arrival; and the set already
    -- carries the resolved weight_kg, which is what every reader actually uses. This records what
    -- the weight was entered against, not a row the set depends on.
    equipment_id TEXT,
    is_warmup    INTEGER NOT NULL DEFAULT 0 CHECK (is_warmup IN (0, 1)),
    notes        TEXT,

    -- Two sets at the same order make "the last set" ambiguous, and the AMRAP is read by position.
    UNIQUE (session_id, set_order)
) STRICT;

-- Synced aggregate root. Rarely edited, so whole-document last-write-wins is ample.
-- Keyed by account and slug together: an equipment id is a short name like "atx", not a GUID, so
-- two accounts that both own an ATX bar would otherwise be one row and overwrite each other.
CREATE TABLE equipment (
    id             TEXT NOT NULL,
    user_id        TEXT NOT NULL REFERENCES users(id),
    name           TEXT NOT NULL,
    kind           TEXT NOT NULL CHECK (kind IN ('Bar', 'AdjustableDumbbell', 'FixedDumbbell', 'Kettlebell', 'Other')),
    unit           TEXT NOT NULL CHECK (unit IN ('Kg', 'Lb')),
    actual_kg      TEXT,
    counts_as_kg   TEXT,
    declared_loads TEXT,
    updated_at     INTEGER NOT NULL,
    device_id      TEXT NOT NULL,
    deleted        INTEGER NOT NULL DEFAULT 0 CHECK (deleted IN (0, 1)),

    PRIMARY KEY (user_id, id)
) STRICT;

-- Child of equipment, replaced wholesale with its parent.
-- Plate weight is stored in grams, not as a TEXT decimal like every other weight in this schema,
-- because here it is part of the key: '5', '5.0' and '05' are three distinct TEXT values and the
-- same plate, and two rows for one plate makes the solver build weights the rack does not have.
CREATE TABLE plates (
    user_id      TEXT NOT NULL,
    equipment_id TEXT NOT NULL,
    kg_g         INTEGER NOT NULL,
    pairs        INTEGER NOT NULL CHECK (pairs > 0),

    PRIMARY KEY (user_id, equipment_id, kg_g),
    FOREIGN KEY (user_id, equipment_id) REFERENCES equipment(user_id, id) ON DELETE CASCADE
) STRICT;

CREATE TABLE settings (
    user_id         TEXT NOT NULL PRIMARY KEY REFERENCES users(id),
    jokers_per_week INTEGER NOT NULL CHECK (jokers_per_week >= 0),
    week_start_day  INTEGER NOT NULL CHECK (week_start_day BETWEEN 0 AND 6),
    updated_at      INTEGER NOT NULL,
    device_id       TEXT NOT NULL
) STRICT;
