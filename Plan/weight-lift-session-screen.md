---
status: "backlog"
tags: [UI]
docs: none
hook: Lift session screen — exercise name, SetCircle row, weight input; the RunScreen counterpart
order: 29
---
# Weight lift session screen

`RunScreen` exists for intervals; there is no equivalent for a lift
session. Needs: exercise name/stage, a row of `SetCircle` per set (done/
active/pending, matching the gallery's existing states), weight input
(per-side aware per habit-system.md §2), rest indicator. Static mock data
first (fixed sets, no timer wiring) — same pattern as `WeekCard`/
`SetCircle` in the gallery today.

* Next step: build after `bottom-nav-shell` lands and gets a visual pass.
* Links: `src/Groot.UI/Components/SetCircle.razor`,
  `src/Groot.UI/Components/RunScreen.razor` (counterpart),
  `design/habit-system.md` §2
