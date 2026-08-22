# Groot — project rules

Rules that were missed at least once during the build. Each one says why it exists.
Decisions and rationale live in research.md (section 10 is the decision log) and
design/habit-system.md; this file is the short version that applies on every change.

## Accessibility (WCAG AA is the floor)

- Text contrast >= 4.5:1, large text and UI graphics >= 3:1, measured against the real
  background token. The first week card shipped at 4.0:1 and 2.7:1. A test over
  `GrootPalette` should fail the build when a text/background pair drops below this.
- Minimum font size: 10px for labels and captions, 12px for running text.
- Every interactive element has a visible `:focus-visible` style and a touch target of
  at least 44px. Icon-only controls carry an `aria-label`.
- Respect `prefers-reduced-motion` on every animation.

## Components and theming

- All markup is a component in `Groot.UI`. Heads (`Groot.Web`, `Groot.App`) and the
  gallery only compose components; they contain no raw UI markup of their own.
- Component CSS uses `var(--g-*)` only. No hex values in component styles. A new colour is
  a new token in `GrootPalette.cs`; `tokens.css` is generated and never edited by hand.
- Use an existing token before adding one (`run-ink` existed and went unused).
- Every component renders correctly in both themes and appears in `tools/Groot.UI.Gallery`
  in every state, light and dark side by side. No gallery entry, no merge.
- MudBlazor for chrome (forms, dialogs, tables, navigation). Custom components only for
  the signature visuals (SetCircle, WeekCard, SeasonGrid, YearRings, timers). Never wrap
  a MudBlazor component in a Groot component.
- MudBlazor providers (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`,
  `MudSnackbarProvider`) live once in `GrootShell`. Hosts never add their own.
- No hardcoded UI strings inside components: text arrives through parameters or resource
  keys. All user-facing copy follows the humanizer checklist (see design/habit-system.md 5b).

## Code (CUPID over SOLID)

- `Groot.Core` has no framework dependencies. Engines are pure functions; their unit tests
  are the specification. Behaviour is composed from records and small interfaces
  (`IProgressionRule`), never from class hierarchies.
- Weights are stored canonically in kg. Bar weight comes from the equipment profile
  (`actual_kg`, `counts_as`), never from a 20 kg assumption. The unit belongs to the
  equipment, not to the app.
- Product copy contains no plant or tree language and nothing adjacent to the a character
  character. Abstract rings and cells only.
- `TreatWarningsAsErrors` stays on.

## Working in this repo (several sessions run in parallel)

- `git pull --rebase` before every commit. Keep commits small and scoped to one change.
- Never hand-edit generated files (`tokens.css`, scoped CSS bundles, `obj/`).
- `dotnet build` takes one project per invocation. Restore new tool projects once.
- After rebuilding a WASM project, restart its preview server (the boot manifest is
  fingerprinted) or run it under `dotnet watch`.
- The NuGet cache lives at `D:\DataStorage\.nuget` (set through `NUGET_PACKAGES`), not under
  `~/.nuget`. Resolve it with `dotnet nuget locals global-packages --list` instead of guessing.
- Deleting files or folders: use `Move-Item` to the session scratchpad. The shell delete
  guard on this machine blocks `Remove-Item` and `rmdir` with false positives.
