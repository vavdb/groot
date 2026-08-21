---
status: "backlog"
tags: [Android, Run, Timer]
docs: design/habit-system.md
hook: Run-interval notification-shade variant, distinct from the lift rest timer
order: 33
---
# Run interval notification-shade variant

Notification shade needs a run-interval variant next to the lift
rest-timer variant (habit-system.md §5.5) — segment/cue state (run/walk),
not a plain countdown. Depends on `android-foreground-rest-timer` for the
underlying chronometer-notification mechanism.

* Links: `design/habit-system.md` §5.5, §3.1
