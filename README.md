# Groot

A workout logger and habit tracker for lifters who also run. Web (Blazor WASM PWA), Android, and
iOS from one C# codebase (.NET MAUI Blazor Hybrid, MudBlazor for UI, Dapper on SQLite,
and a small self-hosted ASP.NET Core API for sync).

**Status: design + early implementation.** The design documents and mockups are the current
visual contract; the domain engines (progression, plate math, intervals, habit contract) are
implemented and unit-tested, and the component library (`Groot.UI`) is starting to render the
mockups — see the screenshots below.

## Screenshots

Component gallery — `SetCircle` and `WeekCard` in light and dark, rendered live from
`tools/Groot.UI.Gallery`:

![Component gallery: SetCircle + WeekCard, light and dark](docs/screenshots/gallery.png)

Design mockups (interactive HTML lives in [`design/`](design/), renders below):

| Habit contract · season grid · year rings | Growth Rings identity (light + dark) | 0→5K interval runner |
|---|---|---|
| ![habit-rings mockup](docs/screenshots/mockup-habit-rings.png) | ![growth-rings mockup](docs/screenshots/mockup-growth-rings.png) | ![run-05k mockup](docs/screenshots/mockup-run-05k.png) |

## What it does (planned)

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
| `docs/screenshots/` | rendered previews of the gallery and the design mockups |

## Building

```bash
dotnet test tests/Groot.Core.Tests            # domain engines, no workloads needed
dotnet build src/Groot.Web                    # Blazor WASM PWA
dotnet build src/Groot.App -f net10.0-windows10.0.19041.0   # Windows head (needs: dotnet workload install maui)
```

Android head needs the Android SDK + JDK once:
`dotnet build src/Groot.App -f net10.0-android -t:InstallAndroidDependencies` (or install via
Visual Studio). iOS builds on a Mac with the maui workload. CI builds everything except
`Groot.App`; the heads are verified through the web head and the component gallery
(`dotnet run --project tools/Groot.UI.Gallery`), the app on a device or emulator.

Before a commit: `dotnet test tests/Groot.Core.Tests`, `dotnet test tests/Groot.UI.Tests`
(palette contrast), and `bash tools/check-rules.sh` (the mechanical half of `AGENTS.md`).

## Credits

- GZCLP is based on the GZCL method by Cody Lefever.
- Exercise media: [exercises-dataset](https://github.com/vavdb/exercises-dataset) (MIT) and
  [free-exercise-db](https://github.com/yuhonas/free-exercise-db) (public domain).

## License

Apache 2.0 — see [LICENSE](LICENSE).
