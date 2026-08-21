---
status: "backlog"
tags: [Android, Timer]
docs: research.md
hook: Android foreground service + chronometer notification for the rest timer — not started
order: 32
---
# Android foreground-service rest timer

The stickiest MVP requirement (research.md §4.2), not started. Foreground
service keeps the process alive through Doze; notification built with
`SetUsesChronometer(true)` + `SetChronometerCountDown(true)` renders a
live countdown with zero notification updates. Actions: "Done set" /
"+30s" / "Skip" via `PendingIntent`. End-of-rest alert via
`AlarmManager.SetExactAndAllowWhileIdle`. Test early on real hardware —
OEM battery killers (Samsung/Xiaomi) are the landmine.

* Links: `research.md` §4.2
