---
status: "done"
tags: [Equipment, Core]
docs: none
hook: PlateSolver computes achievable bar loads from a plate inventory
order: 21
shipped: 2026-08-18
---
# Plate calculator engine

`Groot.Core/Equipment/PlateSolver.cs` — `AchievableTotals` computes every
reachable bar load from a `PlatePair` inventory (plates load in pairs), so
the progression engine never proposes a weight the rack can't build.
Tested in `PlateSolverTests.cs`.

* Shipped in scaffold commit `a1a7860`.
* Feature-matrix MVP row (research.md §3.4): done.
