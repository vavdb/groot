---
status: "backlog"
tags: [Contract, Decision]
docs: design/habit-system.md
hook: Five open design decisions on the habit contract — pick before building the UI
order: 40
---
# Habit contract open questions

From habit-system.md §7, unresolved:

1. Overgrowth (7/7, no rest): warn-only (recommended) or hard-break the ring?
2. Joker auto-spend at ring close vs. manual-only? (recommended: auto with pre-play option)
3. Do run *and* lift on the same day both credit? (recommended: yes, still once per type per day)
4. Grid horizon on phone: 26 weeks visible (recommended) vs full 52 with horizontal scroll?
5. "0→5K" naming vs licensing an actual C25K brand — descriptive-only for now?

Blocks `home-contract-card-screen` (grid horizon, overgrowth display) and
`progress-year-rings-screen` (joker/overgrowth rendering).

* Links: `design/habit-system.md` §7
