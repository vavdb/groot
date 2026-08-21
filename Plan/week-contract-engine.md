---
status: "done"
tags: [Contract, Core]
docs: design/habit-system.md
hook: Pure C# week-contract evaluator — lift/run credits, jokers, rest, overgrowth
order: 20
shipped: 2026-08-18
---
# Week contract engine

`Groot.Core/Contract/WeekContract.cs` — `ContractEvaluator.Evaluate` reduces
a week's sessions to `WeekEvaluation`: lift/run credits (once per calendar
day), rest satisfied by any session-free day, jokers fill missing credits
(never rest), 7/7 training flags overgrowth. Pure and deterministic, tested
in `ContractEvaluatorTests.cs`.

* Shipped in scaffold commit `a1a7860`.
* Links: `design/habit-system.md` §1, §6
