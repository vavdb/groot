# Groot

Barbell sets and interval runs in one log, with a weekly contract that says what counts as a
kept week. Web (Blazor WASM PWA), Android, and
iOS from one C# codebase: .NET MAUI Blazor Hybrid, MudBlazor for UI, Dapper on SQLite for the
device store, and our own self-hosted ASP.NET Core Minimal API for sync. No hosted backend.

**Status: the screens run, the store persists, no sync yet.** The domain engines (progression,
plate math, intervals, the week contract) are implemented and unit-tested. `Groot.UI` renders
every screen, and `Groot.Data` writes them to SQLite. What is missing is the wiring between the
two — the screens still open on their in-memory defaults — plus `Groot.Api`, settings, and a
Progress screen. 277 tests; [`docs/test-coverage.md`](docs/test-coverage.md) says what they cover
and what they do not.

## Screenshots

Every screen below is the real component rendered at 390×844, captured from
`tools/Groot.UI.Gallery`, light and dark.

| | Light | Dark |
|---|---|---|
| **Home** — the week contract, the streak, six months of history | ![Home, light](docs/screenshots/phone-home-light.png) | ![Home, dark](docs/screenshots/phone-home-dark.png) |
| **Lift** — GZCLP day A1, one tap per set | ![Lift, light](docs/screenshots/phone-lift-light.png) | ![Lift, dark](docs/screenshots/phone-lift-dark.png) |
| **Run** — 0→5K week 1, spoken cues | ![Run, light](docs/screenshots/phone-run-light.png) | ![Run, dark](docs/screenshots/phone-run-dark.png) |

The lift screen shows why a working weight is keyed by exercise *and* tier: GZCLP trains squat as
T1 at 60 kg for 5x3+ and, two days later, as T2 for 3x10 at a weight of its own.

Component gallery — every component, every state, both themes:

![Component gallery: SetCircle + WeekCard, light and dark](docs/screenshots/gallery.png)

Design mockups (interactive HTML lives in [`design/`](design/)):

| Habit contract · season grid · year rings | Identity study (light + dark) | 0→5K interval runner |
|---|---|---|
| ![habit-rings mockup](docs/screenshots/mockup-habit-rings.png) | ![growth-rings mockup](docs/screenshots/mockup-growth-rings.png) | ![run-05k mockup](docs/screenshots/mockup-run-05k.png) |

## What it does

Built and tested:

- **The engines.** GZCLP progression including the fail ladder and its reset, plate maths against
  a real rack, interval schedules with cue points, and the weekly contract.
- **The screens.** Home, lift, run, boot, and the empty states, in both themes.
- **The store.** SQLite on the device: sessions and their sets, equipment, settings. Working
  weights are not stored — they are replayed from the logged sessions, so a rule change takes
  effect at once and a session arriving from another device recomputes rather than going stale.

Planned:

- Logs barbell training with one-tap sets, per-side plate entry (`bar 10 + 2×25 = 60 kg`), and
  automatic linear progression (GZCLP-style, with fail-ladders).
- Coaches interval running (0→5K) with spoken cues and countdown notifications, screen off.
- Tracks a weekly habit contract: 2 lifts + 2 runs + 1 rest closes the week; 2 jokers cover the
  gaps. Streaks count weeks, not days.
- Syncs between devices through a small self-hosted API (`Groot.Api`, planned). Accounts are a
  username, no personal data.
- Writes workouts to Health Connect and reads sleep back for recovery context.

## Repo layout

| Path | Contents |
|---|---|
| `AGENTS.md` | the rules for every change, for agents and people alike (`CLAUDE.md` imports it) |
| `research.md` | main research + decision log |
| `design/` | current design: habit system spec, mockups (open the HTML files in a browser) |
| `Research/` | program catalog, ads research, rejected design directions with provenance |
| `data/programs/` | machine-readable program definitions (GZCLP rack edition, 0→5K) |
| `data/log.csv` | training log in Strong-compatible CSV format |
| `docs/screenshots/` | rendered previews of the screens, the gallery and the design mockups |
| `docs/test-coverage.md` | what every test covers, what a failure would mean, and what is untested |
| `Plan/` | one card per piece of work, with what was decided and what was deferred |
| `src/Groot.Core` | the engines: pure functions, no framework, tests are the spec |
| `src/Groot.Data` | SQLite store: schema, connection factory, one repository per aggregate |
| `src/Groot.UI` | components and design tokens, shared by every head |

## Building

```bash
dotnet test tests/Groot.Core.Tests            # domain engines, no workloads needed
dotnet test tests/Groot.Data.Tests            # the store, against a real SQLite file
dotnet build src/Groot.Web                    # Blazor WASM PWA
dotnet build src/Groot.App -f net10.0-windows10.0.19041.0   # Windows head (needs: dotnet workload install maui)
```

Android head needs the Android SDK + JDK once:
`dotnet build src/Groot.App -f net10.0-android -t:InstallAndroidDependencies` (or install via
Visual Studio). iOS builds on a Mac with the maui workload. CI builds everything except
`Groot.App`; the heads are verified through the web head and the component gallery
(`dotnet run --project tools/Groot.UI.Gallery`), the app on a device or emulator.

Before a commit: `dotnet test tests/Groot.Core.Tests`, `dotnet test tests/Groot.Data.Tests`,
`dotnet test tests/Groot.UI.Tests` (palette contrast and component rendering), and
`bash tools/check-rules.sh` (the mechanical half of `AGENTS.md`).

## Credits

- GZCLP is based on the GZCL method by Cody Lefever.
- Exercise media: [exercises-dataset](https://github.com/vavdb/exercises-dataset) (MIT) and
  [free-exercise-db](https://github.com/yuhonas/free-exercise-db) (public domain).

## License

Apache 2.0 — see [LICENSE](LICENSE).
