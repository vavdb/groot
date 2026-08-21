---
status: "done"
tags: [UI, Shell]
docs: none
hook: Mobile bottom nav (Run/Home/Lift/Progress) — static, previewed in the gallery, both themes
order: 28
shipped: 2026-08-21
---
# Bottom nav shell

Phone head (`Groot.App`) runs `GrootShell ShowDrawer="false"` with no
navigation chrome — only the web head's side drawer existed. Built the
phone-shaped counterpart: `BottomNav` (`Groot.UI/Theme`), four items —
Run first (the one screen that's actually functional today), then
Home / Lift / Progress — icon + label, active-state highlight.

Corrections along the way:
* MudBlazor 9.8 has no `MudBottomNavigation` at all (checked the DLL) —
  custom markup, same "no equivalent" rule as `DaySlots`.
* First attempt wrapped the root in `MudPaper`; its rendered root doesn't
  reliably carry this file's CSS-isolation scope attribute (confirmed the
  same gap pre-exists on `WeekCard`/`MudCard`, untouched, separate issue)
  — switched to a plain `<div>` root, matching `DaySlots`.
* Swept all `font-size: Npx` to `rem` across `Groot.UI` while in there
  (`DaySlots`, `RunScreen`, `RunFlood`, `SetCircle`, `BottomNav`) — `px`
  ignores browser text-zoom/OS font-scaling (WCAG 1.4.4).
* Covered by `tests/Groot.UI.Tests/BottomNavTests.cs` (bUnit) before
  landing — render, active-state marking, click → `OnSelectedChanged`.

Not done yet: actually wiring `BottomNav` into `GrootShell`/`MainLayout`
for the phone head — still gallery-only. Rolls into whichever card next
needs real routes to switch between (`weight-lift-session-screen`,
`home-contract-card-screen`, `progress-year-rings-screen`).

* Links: `src/Groot.UI/Theme/BottomNav.razor`,
  `tests/Groot.UI.Tests/BottomNavTests.cs`
