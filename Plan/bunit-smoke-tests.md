---
status: "done"
tags: [Testing, UI]
docs: none
hook: bUnit smoke tests — render every routable page + key components, assert no exception
order: 27
shipped: 2026-08-21
---
# bUnit smoke tests

No test coverage existed for `Groot.UI` — CI only ran `dotnet test` on
`Groot.Core.Tests` (pure domain logic). `dotnet build` compiles Razor
clean even when it's broken at render: three bugs shipped invisibly this
way in one sitting (2026-08-21) — `Home.razor` missing `@page "/"` on both
heads, `GrootAudioControls` throwing on `@bind-Language`/`@bind-Sound`
(missing `LanguageChanged`/`SoundChanged` callbacks), `GrootShell` missing
`<MudPopoverProvider />` breaking every dropdown. All three were only
found by actually running the app with Playwright, not by CI.

`tests/Groot.UI.Tests` (bUnit 2.9 + xUnit) now covers:
* `BottomNavTests` — renders all four destinations, marks the selected
  one active, raises `OnSelectedChanged` on click.
* `RegressionTests` — one guard per bug above: `Home.razor` has a `"/"`
  `RouteAttribute`; `GrootShell` renders `.mud-popover-provider`;
  `GrootRunScene` with `ShowAudioControls` doesn't throw; changing the
  `Voice` select actually raises `LanguageChanged`.

Wired into `.github/workflows/ci.yml` as a normal `dotnet test` step next
to the Core test step, and added to `Groot.slnx`.

Gotchas hit building this (left as comments in the test file): MudBlazor
registers `IAsyncDisposable`-only services, so the test class needs
`IAsyncLifetime` or xUnit's default sync `Dispose()` throws on teardown;
`MudSelect` opens on `mousedown` against `.mud-input-control`, not
`click`; its popover content renders into a separately-rendered
`MudPopoverProvider`, not under the select's own component tree.

Playwright/E2E stays a separate, heavier follow-on (catches CSS/visual
and scope-attribute-class issues bUnit can't) — not in scope here.

* Links: `.github/workflows/ci.yml`, `tests/Groot.UI.Tests/`, commit
  `a50d954`
