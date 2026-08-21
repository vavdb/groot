---
status: "backlog"
tags: [Testing, UI]
docs: none
hook: bUnit smoke tests — render every routable page + key components, assert no exception
order: 27
---
# bUnit smoke tests

No test coverage exists for `Groot.UI` — CI only runs `dotnet test` on
`Groot.Core.Tests` (pure domain logic). `dotnet build` compiles Razor
clean even when it's broken at render: three bugs shipped invisibly this
way in one sitting (2026-08-21) — `Home.razor` missing `@page "/"` on both
heads, `GrootAudioControls` throwing on `@bind-Language`/`@bind-Sound`
(missing `LanguageChanged`/`SoundChanged` callbacks), `GrootShell` missing
`<MudPopoverProvider />` breaking every dropdown. All three were only
found by actually running the app with Playwright, not by CI.

bUnit gives fast, in-process component rendering — no browser — so it
fits as a normal `dotnet test` CI step:
* Render every routable page (`Home`, `Run`) with `GrootShell`, assert no
  exception.
* Render `GrootRunScene`/`GrootAudioControls` together, assert the
  `@bind-*` wiring doesn't throw (would have caught today's crash).
* Assert `MudPopoverProvider` (or equivalent) is present in the shell.

Playwright/E2E is a separate, heavier follow-on (catches CSS/visual and
scope-attribute-class issues bUnit can't) — not in scope for this card.

* Next step: add a `tests/Groot.UI.Tests` project (bUnit + xUnit), wire
  into `.github/workflows/ci.yml` alongside the existing Core test step.
* Links: `.github/workflows/ci.yml`, commit `a50d954`
