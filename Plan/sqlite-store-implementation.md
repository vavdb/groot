---
status: "backlog"
tags: [Data, Core]
docs: design/habit-system.md
hook: Groot.Data implementation — SQLite schema, Dapper repositories, and the heads writing to it
order: 11
---
# SQLite store: the implementation

Backend decided (`backend-path-spike`): SQLite on the device, our own
`Groot.Api` for sync. This card is the device half — the point at which a
session survives a reload. Everything on the screens is component state
today: log five sets, refresh, they are gone.

## Schema (habit-system.md §6)

GUID primary keys, `updated_at`, and a `deleted` tombstone on every table,
because the same rows sync later and last-write-wins needs both.

```
users(id, username, created_at)
programs(id, user_id, program_id, version, json, updated_at, deleted)
weeks(id, user_id, week_start, contract_met, jokers_spent, overgrowth, closed_at, updated_at)
sessions(id, user_id, date, kind: lift|run|rest_claim, program_id?, day_key?,
         duration_s?, notes?, updated_at, deleted)
sets(id, session_id, exercise_id, set_order, weight_kg, reps, entry_mode,
     entry_weight, entry_unit, equipment_id?, is_warmup, updated_at, deleted)
exercise_state(user_id, exercise_id, working_weight_kg, stage, last_base_weight_kg, updated_at)
equipment(id, user_id, name, kind, unit, actual_kg?, counts_as_kg?, declared_loads?)
plates(equipment_id, kg, pairs)
settings(user_id, jokers_per_week, week_start_day, updated_at)
```

`sets` mirrors `Groot.Core/Sessions/SetEntry.cs`, which already models the
three entry modes and has tests. `exercise_state` is what
`LiftProgressionPlanner` reads and writes: it is the reason the lifting
screen can only guess at the ladder stage today.

## Shape

- `Groot.Data` gets: a connection factory (one file-scoped SQLite database,
  WAL), an embedded `schema.sql` applied on open with a `user_version`
  check, and one repository per aggregate (`SessionStore`, `ProgramStore`,
  `EquipmentStore`, `SettingsStore`). Dapper, parameterised, no ORM.
- No repository interface until a second implementation exists. The web
  head is the second implementation (IndexedDB or `sqlite3.wasm`,
  `web-offline-indexeddb`), so the seam arrives with it, not before.
- `Groot.Core` stays framework-free: it keeps taking and returning records,
  and never learns what a connection is.

## What it unblocks

- `LiftScreen` and `RunScreen` write what they log, and read back the
  working weight and ladder stage instead of a starting table.
- `home-contract-card-screen`: the week card and the streak read real
  weeks, and `GrootLanding.Streak` stops being a parameter nobody fills.
- `EquipmentProfile.Rack` becomes a row the settings screen edits, which
  is what retires the last hardcoded rack in the UI.
- `progress-year-rings-screen`, `csv-import-strong-format`,
  `manual-run-entries-and-notes` — all need somewhere to write.

## Done means

- A session logged on the phone survives a restart, and on the web head
  survives a reload.
- Schema applied from `schema.sql`, versioned; opening an older database
  migrates it forward and a test covers the upgrade.
- Store tests run against a real SQLite file in a temp directory, not a
  mock: round-trip every entry mode, tombstones, and the `updated_at`
  ordering sync will depend on.
- No raw SQL outside `Groot.Data`; no `DateTime.Now` in a store (times come
  in as parameters, so the tests can pin them).

* Next step: `schema.sql` plus the connection factory and `SessionStore`,
  wired into `LiftScreen` behind a store interface the scene owns. One
  aggregate at a time; the lifting session is the one that hurts most.
* Links: `design/habit-system.md` §6, `research.md` §5.2 and §5.4,
  `Plan/backend-path-spike.md`, `Plan/web-offline-indexeddb.md`
