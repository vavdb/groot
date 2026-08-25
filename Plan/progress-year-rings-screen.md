---
status: "backlog"
tags: [UI, Contract]
docs: design/habit-system.md
hook: Progress tab, five candidates rendered in the gallery, the metaphor still open
order: 31
---
# Progress / year-rings screen

Progress tab: a lifetime view plus the full season grid. The metaphor has moved
twice, from a tree cross-section to a barbell seen head on (2026-08-21), and
head on has now been drawn and does not survive being looked at: plates hide
behind each other, so a year at 118,000 kg and one at 341,000 kg draw nearly the
same disc, and only the widest plate reads.

Five candidates now render against the same data in both themes at
`/progress` in `tools/Groot.UI.Gallery`:

- **A, loaded bar side on.** The truest metaphor, a middling chart: the plates
  cluster at the left and the sleeve runs on past them.
- **B, head on.** The 2026-08-21 decision. Drawn fairly and still unreadable.
- **C, plate column.** Reads fastest. Heights on one baseline, survives a phone.
- **D, year strip.** The season grid, one row per year. The only one that keeps
  *when* things happened, so a gap sits on the weeks it cost.
- **E, session recap.** C moved to the end of a session, where the plates are
  bounded and real. `PlateSolver` answers what the bar actually held, counted
  once per bar and again once per set.

Open: C paired with D looks like the answer, and "year rings" is the wrong name
for any of them, since none is a ring. E has a layout problem to solve first,
because counting plates per set makes a tall stack on a heavy day.

* Next step: pick one from the gallery, then build it as a component in
  `Groot.UI` and rename it away from "rings".
* Links: `design/habit-system.md` §4 Concept C (2026-08-21 update), §5.4
