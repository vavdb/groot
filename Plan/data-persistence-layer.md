---
status: "backlog"
tags: [Data]
docs: design/habit-system.md
hook: Groot.Data is an empty project — no schema, no local store yet
order: 35
---
# Data persistence layer

`Groot.Data` has no `.cs` files. Needed: local store (SQLite) for
weeks/sessions/sets/programs/settings per the data model delta
(habit-system.md §6):

```
weeks(id, user_id, week_start_date, contract_met, jokers_spent, overgrowth, closed_at)
sessions(id, user_id, date, type: wl|run|rest_claim, program_id?, duration_s?, ...)
sets: + entry_mode, bar_kg?, side_kg?
programs: + type: sets_reps | intervals
settings: jokers_per_week (default 2), week_start (locale default, overridable)
```

Blocks `home-contract-card-screen`, `progress-year-rings-screen`,
`csv-import-strong-format`, `manual-run-entries-and-notes` — all need
somewhere to write to.

* Links: `design/habit-system.md` §6
