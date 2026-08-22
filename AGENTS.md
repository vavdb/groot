# Groot — rules for every agent

Read this before changing anything. It applies to Claude, Codex, GLM and humans alike. Each
rule says why it exists; most were missed at least once. Decisions and rationale live in
research.md (section 10 is the decision log) and design/habit-system.md; this is the short
version that applies on every change. `bash tools/check-rules.sh` checks the mechanical ones.

## State (2026-08-22)

Works end to end, both heads: the 0→5K interval runner (`RunScreen`) and the lifting screen
(`LiftScreen`, GZCLP: sets, plate maths, rest, AMRAP, missed sets, and what the next session
becomes). Done and unit-tested: the engines in `Groot.Core` (progression, plate math, week
contract, intervals, program catalog, lift programs and the progression planner). The gallery
renders every component and, on `/screens`, every screen in a device frame.
Not started: persistence (`Groot.Data` is an empty csproj), `Groot.Api`, sync, settings,
resource-based i18n. Nothing a session logs survives a reload, and the lifting screen's working
weights and equipment are stand-ins until there is a store. README and research.md describe the
planned shape; do not assume it exists.

## Commands

| What | Command |
|---|---|
| Domain tests, under a second; run before every commit | `dotnet test tests/Groot.Core.Tests` |
| Palette contrast and type-scale tests | `dotnet test tests/Groot.UI.Tests` |
| Rule checks, the same ones CI runs | `bash tools/check-rules.sh` |
| Build the component library; regenerates `tokens.css` | `dotnet build src/Groot.UI` |
| Build the web head, the gallery | `dotnet build src/Groot.Web`, `dotnet build tools/Groot.UI.Gallery` |
| Preview servers (`.claude/launch.json`) | `groot-web` :5063, `groot-gallery` :5200; `groot-web-b` :5064, `groot-gallery-b` :5201 when another session holds the port |

`dotnet build` takes one project per invocation (MSB1008 otherwise). `Groot.App` builds only
on a machine with the MAUI workload and the Android SDK; CI skips it. Verify UI work in the
gallery and the web head; the owner checks the app on the emulator.

## Where things live

| Thing | Path |
|---|---|
| Engines: pure functions, tests are the spec | `src/Groot.Core/{Programs,Contract,Equipment,Intervals,Sessions}`, `tests/Groot.Core.Tests` |
| Components: one `.razor`, its `.razor.css`, a state record | `src/Groot.UI/Components` |
| Design tokens, single source | `src/Groot.UI/Theme/GrootPalette.cs`: `All` colours, `Scale` fonts and sizes, `MudRoles` MudBlazor mapping. Generated: `wwwroot/tokens.css` |
| MudBlazor theme (C# side of the same roles), app shell | `src/Groot.UI/Theme/GrootTheme.cs`, `GrootShell.razor` |
| Program definitions, embedded into Core | `data/programs/*.json`; a new program is a new file plus catalog tests |
| Spoken cue copy, en + nl (stand-in for resources) | `src/Groot.Core/Intervals/RunCueText.cs` |
| Heads: they compose components and nothing else | `src/Groot.Web/Pages`, `src/Groot.App/Components/Pages` |
| Gallery: every component, every state, light and dark | `tools/Groot.UI.Gallery/Pages/GalleryColumn.razor` |
| Decisions, habit spec, copy voice, IP position | `research.md` §10, `design/habit-system.md` (§5b is copy), `Research/UI/README.md` |
| Multi-model implement/review loop | `docs/ai-workflow.md` |

## Done means

### Component

- Gallery entry in `GalleryColumn.razor`: every state, light and dark side by side.
- Contrast measured against the real background token: text 4.5:1, large text and UI graphics
  3:1. Amber and clay as text or thin stroke use `amber-text` / `clay-text` (the light theme
  darkens them); as fills they keep `run-ink` / `card` on top. A new foreground/background
  pair is a new row in `tests/Groot.UI.Tests/PaletteContrastTests.cs`.
- `:focus-visible` on every custom interactive element, touch targets of 44px or more,
  `aria-label` on icon-only controls, `prefers-reduced-motion` on every animation.
- CSS uses `var(--g-*)` only: no colour literal (hex, rgb, rgba), no px font size, no font
  family name. A tint is `color-mix(in srgb, var(--g-amber) 15%, transparent)`. A new colour
  or size is a new token in `GrootPalette.cs`; then `dotnet build src/Groot.UI` and commit
  `tokens.css`. Use an existing token before adding one.
- Scoped CSS reaches only elements the component renders itself. Blazor puts the scope
  attribute on the last selector of a rule and never on a MudBlazor component's root, so a
  plain `.x` rule for `<MudText Class="x">` is dead (the week card shipped without its
  max-width that way). Own root element first (`<div class="week-card">`), then
  `::deep .jokers` for MudBlazor children.
- Strings enter through parameters or resource keys, with a sensible default. The resource
  pipeline is not built yet; until it is, no concatenation that breaks for Dutch.
- When markup moves into a sub-component, its CSS moves with it; nothing stays behind.

### Engine

- Lives in `Groot.Core`, no framework usings. Records and small interfaces (`IProgressionRule`),
  composed, never inherited. Deterministic; the xunit tests name the behaviour.
- Weights in kg, canonically. Bar weight comes from the equipment profile (`ActualKg`,
  `CountsAsKg`), never from a 20 kg assumption. The unit belongs to the equipment.

### Copy (every user-visible or spoken string)

- Humanizer pass, design/habit-system.md §5b: say the concrete thing, numbers over adjectives,
  no em dashes (a TTS pause is a comma or a period), no aphorisms, no negative parallelisms.
- No plant or tree language, nothing adjacent to the a character character. Abstract rings and
  cells only. Palette token names (moss, bark) are internal and stay.

### Every commit

- `dotnet test tests/Groot.Core.Tests` and `bash tools/check-rules.sh` pass.
- `tokens.css` regenerated and committed whenever `GrootPalette.cs` changed; CI diffs it.
- Conventional commit subject: `feat(ui): …`, `fix(run): …`, `test(core): …`, `docs: …`,
  `chore: …`, `ci: …`. One change per commit.

## Code conventions

- `TreatWarningsAsErrors` stays on. `.editorconfig` is the formatting authority: spaces, LF.
- XML `<summary>` on public types and members is house style. Keep it; do not strip it.
- `async Task`, never `async void`; `EventCallback` and `@bind:after` await it.
- `sealed record` for data, `static class` for engines, file-scoped namespaces.
- MudBlazor for chrome: forms, dialogs, tables, navigation, buttons. Custom components only for
  the signature visuals (SetCircle, WeekCard, SeasonGrid, YearRings, timers). No pass-through
  wrappers (a `GrootButton` whose only job is to re-expose `MudButton`); composing MudBlazor
  inside a Groot component is the norm.
- MudBlazor providers live once in `GrootShell`; hosts never add their own.
- Replacing a `RunSession` instance needs `@key` on `RunScreen` (see its doc comment).

## Known gaps (owner calls; do not fix silently)

- MudBlazor `Color.Primary` as text or outline (`Variant.Text`, `Variant.Outlined`) is amber on
  the page: 2.5:1 in the light theme. Use `Variant.Filled` for primary actions until a
  text-variant mapping exists.
- Both heads load Fraunces and Public Sans from Google Fonts; the MAUI app has no self-hosted
  copy, so it falls back to system fonts offline.
- The gallery's page chrome is raw markup by design. Product heads stay component-only.

## Working in this repo (several sessions run in parallel on `main`)

- `git status` first. Files dirty that you did not touch mean another session is live there;
  stay out of them.
- `git pull --rebase` before every commit. A rebase conflict in a file you did not change: stop
  and report, do not resolve someone else's work.
- Small commits, one change each, pushed soon after; the other sessions rebase onto them.
- Never hand-edit generated files (`tokens.css`, `*.styles.css`, `obj/`, `bin/`).
- After rebuilding a WASM project, restart its preview server (the boot manifest is
  fingerprinted) or run it under `dotnet watch`.
- Deleting: move files to a scratch folder instead of `rm`. On the Windows box a shell guard
  blocks `Remove-Item` and `rmdir` with false positives.
- Reviewers (Codex, GLM) get the diff, this file, and the previous reviewer's findings, so they
  verify fixes instead of re-reporting. Templates in docs/ai-workflow.md.
