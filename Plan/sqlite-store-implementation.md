---
status: "doing"
tags: [Data, Core]
docs: design/habit-system.md
hook: Groot.Data is built and tested; the run screen reads and writes it, the lifting screen does not
order: 11
---
# SQLite store: the implementation

Backend decided (`backend-path-spike`): SQLite on the device, our own
`Groot.Api` for sync. This card is the device half, the point at which a
session survives a reload.

The store is built and tested. Half the wiring landed on 2026-08-31: the run
screen on the App head writes a finished run through `GrootStorage`
(`heart-rate-and-route-capture`). The lifting screen is still component
state, so five sets logged and a refresh still loses them.

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
  `manual-run-entries-and-notes`, all need somewhere to write.

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

## Built (2026-08-23)

`schema.v1.sql`, `GrootDatabase`, and stores for sessions, equipment, settings
and users. Working weights are not stored: `LiftProgressionHistory` replays the
logged sessions, so `exercise_state` never got built. `weeks`, `programs` and a
sync cursor are not there either, for the same reason: nothing writes them yet.

Two write paths per aggregate. `Save` is unconditional, because a local write is
the newest thing that happened on this device; `Merge` is last-write-wins on
`(updated_at, device_id)` and refuses a row that claims a different owner.

## Decided later, on purpose

- **Program versioning.** A session records `program_id` and `day_key`, not which
  version of the program it was performed under, and `data/programs/*.json` is
  editable. Editing a shipped program rewrites every historical working weight it
  touches. The fix is a `program_version` column plus a catalog that keeps old
  versions; until then, a shipped program is not edited in place.
- **Materialised `weeks`.** The contract is evaluated live from sessions, which is
  right, but it reads `week_start_day` and `jokers_per_week` from settings. Change
  either and closed weeks reshape. habit-system.md §6 said "materialised at week
  close" for exactly this; it lands with the week-close flow.
- **A stored weight override.** A lifter who corrects a working weight without
  training has nowhere to put it: the replay only knows what was logged. That is an
  input, not a projection, and it arrives with the settings screen.
- **The pull cursor.** `updated_at` resolves conflicts; it must not also be the sync
  cursor. A server-assigned `server_seq` lands with `Groot.Api`.

* Next step: wire `GrootLiftScene` to `GrootStorage`, the way `Run.razor` already
  wires `GrootRunScene`. That is what retires the starting table and lets
  `LiftProgressionHistory` replay real sessions instead of seeded ones.
* Links: `design/habit-system.md` §6, `research.md` §5.2 and §5.4,
  `Plan/backend-path-spike.md`, `Plan/web-offline-indexeddb.md`
