# Groot — Program Catalog

*Which programs ship, what they're called (per research.md §12 trademark rules), and in which release.
MVP definitions below are complete enough to implement `Groot.Core` against.*

## Roadmap

| Ship name | Based on | Type | Release | Notes |
|---|---|---|---|---|
| **GZCLP (rack edition)** | GZCL method, Cody Lefever (free, credit) | sets_reps | **MVP** | the owner's program — see §1 |
| **0→5K** | generic couch-to-5K interval progression | intervals | **MVP** | see §2 |
| Classic 5×5 LP | 5×5 linear progression (public domain, Reg Park lineage) | sets_reps | v1.1 | never named "StrongLifts" |
| AMRAP LP | GSLP-style / Phrak's variant (community) | sets_reps | v1.1 | never named "Greyskull" |
| nSuns LP (5-day) | nSuns 531-style spreadsheets (free, credit) | sets_reps | v1.2 | needs cycle engine (percent-of-TM) |
| lvysaur 4-4-8 | free reddit program (credit) | sets_reps | v1.2 | user trained it before |
| Custom builder | — | both | core feature | "Vin1" proves the need; builder is not a program but makes all others editable copies |
| Madcow 5×5 / 531 BBB / bodyweight tier | various | sets_reps | later | demand-driven |

In-app "About these methods" screen credits every author with links. All instruction prose self-written.

**Machine-readable definitions live at [`data/programs/gzclp-rack.json`](../data/programs/gzclp-rack.json)
and [`data/programs/0-to-5k.json`](../data/programs/0-to-5k.json)** — embedded resources in MVP;
MVP++ serves the same files versioned from the VPS so programs update without an app release
(research.md §13.6).

---

## §1 MVP lifting program — **GZCLP (rack edition)**

Profile: newbie-gains linear progression, powerlifting focus. Equipment constraint: **barbell + power rack
(with pull-up bar) + bench + dumbbells** — no cables/machines, so GZCLP's usual lat pulldown T3 swaps to
chin-ups/DB rows (standard GZCL-sanctioned substitution). Owner has GZCLP history (375 logged sets) —
familiarity beats novelty for habit re-entry.

### Structure — 4 rotating days, 3 sessions/week (Tue/Thu/Sat template)

| Day | T1 (main, 5×3+) | T2 (secondary, 3×10) | T3 (pump, 3×15+) |
|---|---|---|---|
| A1 | Squat | Bench Press | Chin-ups (AMRAP) |
| B1 | Overhead Press | Deadlift | DB Row |
| A2 | Bench Press | Squat | DB Curl |
| B2 | Deadlift | Overhead Press | DB Lateral Raise |

Rotation continues across weeks (Tue A1 · Thu B1 · Sat A2 · next Tue B2 · …).

### Progression rules (the LP state machine)

- **T1**: 5×3, last set AMRAP. Success → +2.5 kg (squat/bench/OHP) / +5 kg (deadlift).
  Fail (any set short) → next stage same weight: **5×3 → 6×2 → 10×1** → after 10×1 fails, reset to
  85–90% of last 5×3 weight and restart at 5×3. (Classic GZCLP fail-ladder.)
- **T2**: 3×10 → fail → 3×8 → 3×6 → reset +weight at 3×10. +2.5 kg on success.
- **T3**: 3×15, last set AMRAP; total ≥25 reps → +2.5 kg (DB: next increment). Chin-ups: bodyweight
  AMRAP; ≥25 total → add weight (DB between knees / belt).
- Starting weights: from the user's history import (last known 5RM-ish) × ~0.85, or manual.
- **Re-entry after a long layoff (owner's case, decided 2026-08-18): start at ~50–55% of old 5×3
  work weights** — Squat 50–60 · Bench 35–40 · OHP 25 · Deadlift 60 kg — and let the LP ladder climb
  back. Two "too light" weeks buy tendons/joints time; concurrent 0→5K makes the conservative start
  doubly right. The importer should offer both presets: *continuity* (×0.85) and *re-entry* (×0.5).
- Rest suggestions: T1 3 min · T2 2 min · T3 1 min (feeds the rest-timer defaults).

### JSON definition sketch (`programs.json_definition`, type `sets_reps`)

```json
{
  "name": "GZCLP (rack edition)",
  "type": "sets_reps",
  "credit": { "method": "GZCL by Cody Lefever", "url": "http://swoleateveryheight.blogspot.com" },
  "rotation": ["A1", "B1", "A2", "B2"],
  "days": {
    "A1": [
      { "exercise": "squat", "tier": 1, "scheme": "5x3+", "increment": 2.5,
        "failLadder": ["6x2+", "10x1+"], "resetPct": 0.9 },
      { "exercise": "bench-press", "tier": 2, "scheme": "3x10", "increment": 2.5,
        "failLadder": ["3x8", "3x6"], "resetBump": 2.5 },
      { "exercise": "chin-up", "tier": 3, "scheme": "3x15+", "progressAt": 25, "loading": "bodyweight+" }
    ]
    // B1 / A2 / B2 same shape
  },
  "restSeconds": { "1": 180, "2": 120, "3": 60 }
}
```

---

## §2 MVP running program — **0→5K**

9 weeks × 3 sessions. Time-based (no GPS needed). Every session: 5:00 brisk-walk warm-up + 5:00 cool-down.

| Week | Core block | Session total |
|---|---|---|
| 1 | 8 × (run 1:00 · walk 1:30) | 30 min |
| 2 | 6 × (run 1:30 · walk 2:00) | 31 min |
| 3 | 2 × (run 1:30 · walk 1:30 · run 3:00 · walk 3:00) | 28 min |
| 4 | run 3 · walk 1:30 · run 5 · walk 2:30 · run 3 · walk 1:30 · run 5 | 31 min |
| 5* | D1: 3×(run 5 · walk 3) · D2: run 8 · walk 5 · run 8 · D3: **run 20** | varies |
| 6* | D1: run 5 · walk 3 · run 8 · walk 3 · run 5 · D2: 2×(run 10 · walk 3) · D3: run 22 | varies |
| 7 | run 25 (×3) | 35 min |
| 8 | run 28 (×3) | 38 min |
| 9 | run 30 (×3) → 5K | 40 min |

\* weeks 5–6 differ per session (D1/D2/D3) — definition supports per-session overrides.

### JSON definition sketch (type `intervals`, week 3 shown)

```json
{
  "name": "0→5K", "type": "intervals",
  "weeks": [
    { "week": 3, "sessions": [ { "segments": [
      { "kind": "walk", "seconds": 300, "label": "warmup",
        "cues": [ { "at": -15, "key": "cue.warmupEnding" } ] },
      { "kind": "run",  "seconds": 90,
        "cues": [ { "at": 0, "key": "cue.startRun" }, { "at": -10, "key": "cue.endingSoon" } ] },
      { "kind": "walk", "seconds": 90,  "cues": [ { "at": 0, "key": "cue.startWalk" } ] },
      { "kind": "run",  "seconds": 180, "cues": [ { "at": 0, "key": "cue.startRun" } ] },
      { "kind": "walk", "seconds": 180, "cues": [ { "at": 0, "key": "cue.startWalk" } ] },
      { "kind": "repeat", "times": 2, "of": "previous-4" },
      { "kind": "walk", "seconds": 300, "label": "cooldown",
        "cues": [ { "at": 0, "key": "cue.startCooldown" }, { "at": -10, "key": "cue.almostDone" } ] }
    ] } ] }
  ]
}
```

`at`: seconds relative to segment start (≥0) or segment end (<0). Cue keys resolve through the i18n
resource layer with context args (`ordinal` of next run, remaining time), so
`cue.warmupEnding(ordinal=1)` renders **"Almost done with the warm-up — get ready for your first run."**
in `en`, and the Dutch resource renders it in `nl`. Same pipeline as all UI strings — voices follow app locale.

### MVP week template (matches the owner's contract)

Mon 0→5K · Tue GZCLP · Wed 0→5K · Thu GZCLP · Fri 0→5K · Sat GZCLP · Sun rest —
contract stays 2+2+1 with 2 jokers (template is the ideal, contract is the rule).
