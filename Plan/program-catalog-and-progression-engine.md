---
status: "done"
tags: [Programs, Core]
docs: Research/programs.md
hook: Program catalog (JSON-defined) + rule-based progression engine, GZCLP-ready
order: 22
shipped: 2026-08-18
---
# Program catalog and progression engine

`Groot.Core/Programs/ProgramCatalog.cs` loads built-in programs from
embedded `data/programs/*.json` — a new program is a new data file, never
new code. `ProgressionRules.cs` is composition-over-inheritance: an
exercise owns an ordered rule list, first applicable rule decides the next
weight/stage. Tested in `ProgramCatalogTests.cs`, `ProgressionEngineTests.cs`.

* Shipped in scaffold commit `a1a7860`.
* Covers the "rule-per-lift (LP)" feature-matrix row; 531/GZCL cycle
  engines are later (see `program-engine-scope` card).
