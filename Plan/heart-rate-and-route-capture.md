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
* **GPS was out of MVP** (research.md §10, 2026-08-18: "GPS stays out of
  MVP", and MVP+1 was to be GPS-lite with coordinates never stored).
  Coordinates are now stored. That is a scope reversal nobody logged, and
  it has a privacy shape: a route track is a home address. Owner call
  whether to log the reversal in §10, keep routes local and never sync
  them, or make storing them a setting.

* Next step: decide the GPS scope question above, then whether the web
  head gets sensor implementations or a documented "app only".
* Links: `src/Groot.Core/Health`, `src/Groot.App/Platforms/Android`,
  `SessionMetricsStore` · `sqlite-store-implementation.md`
