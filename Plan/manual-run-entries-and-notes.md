---
status: "backlog"
tags: [Run, Data]
docs: data/log.csv
hook: Support manual/custom run entries with duration + free-text notes — no schema field for either yet
order: 15
---
# Manual run entries and notes

Sample data (`data/log.csv`) has a manual 10-minute run logged for
2026-08-20 with note "intro". The Strong-format CSV has no duration or
distance column — the 10 min lives in `Notes` as a workaround, which only
proves the app's own data model has the same gap.

Two related things missing:
1. **Manual/custom run entries** — today runs only come from a structured
   interval program (0→5K driver). A user needs to log a run that wasn't
   run through the interval runner (duration only, no segments).
2. **Notes on runs** — `SetEntry` (lift sets) already carries `Notes`;
   run/interval sessions have no equivalent field.

* Next step: extend the session model (see `sqlite-store-implementation`,
  `csv-import-strong-format` cards) so a run session can be duration-only
  and carry a note, without requiring the full interval segment log.
* Links: `data/log.csv`, `src/Groot.Core/Sessions/SetEntry.cs`,
  `design/habit-system.md` §6 (data model delta)
