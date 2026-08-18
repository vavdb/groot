# Groot

A workout logger and habit tracker for lifters who also run. Web (Blazor WASM PWA), Android, and
iOS from one C# codebase (.NET MAUI Blazor Hybrid).

**Status: design phase.** No app code yet. The design documents and mockups in this repo are the
current deliverables.

## What it does (planned)

- Logs barbell training with one-tap sets, per-side plate entry (`bar 10 + 2×25 = 60 kg`), and
  automatic linear progression (GZCLP-style, with fail-ladders).
- Coaches interval running (0→5K) with spoken cues and countdown notifications, screen off.
- Tracks a weekly habit contract: 2 lifts + 2 runs + 1 rest closes the week; 2 jokers cover the
  gaps. Streaks count weeks, not days.
- Syncs between devices through a self-hosted backend (PocketBase). Accounts are a username, no
  personal data.
- Writes workouts to Health Connect and reads sleep back for recovery context.

## Repo layout

| Path | Contents |
|---|---|
| `research.md` | main research + decision log |
| `design/` | current design: habit system spec, mockups (open the HTML files in a browser) |
| `Research/` | program catalog, ads research, rejected design directions with provenance |
| `data/programs/` | machine-readable program definitions (GZCLP rack edition, 0→5K) |
| `data/log.csv` | training log in Strong-compatible CSV format |

## Credits

- GZCLP is based on the GZCL method by Cody Lefever.
- Exercise media: [exercises-dataset](https://github.com/vavdb/exercises-dataset) (MIT) and
  [free-exercise-db](https://github.com/yuhonas/free-exercise-db) (public domain).

## License

Apache 2.0 — see [LICENSE](LICENSE).
