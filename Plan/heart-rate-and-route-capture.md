---
status: "done"
tags: [Run, Health, Data, Android]
docs: research.md
hook: Heart rate over Bluetooth LE and a GPS route on the run screen, written to the store when a run ends
order: 42
shipped: 2026-08-31
---
# Heart rate and route capture

Shipped 2026-08-31 across five commits (`1cc1cbc`, `f7e8120`, `d931c9f`,
`b90600e`, `f41ada3`). The board never carried a card for it; this one is
written after the fact so the work is on the board and its gaps are named.

## What shipped

* **Core** (`src/Groot.Core/Health`): `HeartRateSample`, `HeartRateTrack`,
  `HeartRateAxis`, `HeartRateZone`, `HeartRateTraceView`, and the route
  side, `RouteFix`, `RouteTrack`, `RouteView`, `Geo`. Pure functions,
  unit-tested (`HeartRateTrackTests`, `RouteTrackTests` and kin).
* **Data**: `schema.v2.sql` and `SessionMetricsStore`, so what a session
  measured has a place to live. `GeoValues` for coordinate round-tripping.
  Tested against a real SQLite file.
* **UI**: `HeartRateTrace` over the run and walk blocks, `RouteMap`,
  `SourceChips` for which radio is feeding the screen and why it is quiet.
  New palette tokens, contrast rows, gallery entries.
* **App**: `AndroidHeartRateService` (BLE Heart Rate Profile),
  `AndroidLocationService`, `BluetoothPermissions`, manifest permissions,
  DI wiring, and `GrootStorage` — which makes the run screen **the first
  head that reads and writes the store**. A finished run persists.
* A fake monitor and a packet spec so the BLE parsing is testable without
  a strap.

## Gaps this left

* **Android only.** `IHeartRateService` and `ILocationService` live in
  `Groot.UI`, and only `Groot.App/Platforms/Android` implements them. The
  web head has no implementation, so the run screen there shows the
  quiet-radio state permanently. Web Bluetooth and the Geolocation API
  could fill it; nobody has decided whether they should.
* **iOS has nothing**, consistent with the rest of the app.
* **The lift screen still does not persist.** `GrootStorage` is wired into
  `Run.razor` only. See `sqlite-store-implementation`.
* ~~GPS was out of MVP.~~ **Settled 2026-09-01: GPS is in MVP and the route
  is stored.** The owner wants the MVP to be the app they take outside.
  Logged in research.md §10 and habit-system.md §3.1. The privacy condition
  that came with it: the route stays on the device, and routes sync only as
  an explicit opt-in when `Groot.Api` reaches the question.

* Next step: decide whether the web head gets sensor implementations or a
  documented "app only". Then pace and distance from the stored route,
  which need outlier rejection before either is shown as a number.
* Links: `src/Groot.Core/Health`, `src/Groot.App/Platforms/Android`,
  `SessionMetricsStore` · `sqlite-store-implementation.md`
