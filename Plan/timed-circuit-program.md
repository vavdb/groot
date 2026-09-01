---
status: "backlog"
tags: [Programs, Core, UI]
docs: research.md
hook: Timed circuit program type — a Tabata-style timer that speaks named movements, reusing the interval engine
order: 41
---
# Timed circuit program (Tabata-style timer)

A third program type beside `sets_reps` and `intervals`: a list of named
movements, each held for a number of seconds, spoken as it starts. The
mobility circuit below is the first one; a real 20/10 Tabata is the same
type with two segment kinds and eight rounds.

## Why it is not one of the two existing types

`sets_reps` is weight and reps, and none of this has either. `intervals`
is the right clock but the wrong vocabulary: `IntervalSegment.Kind` is
`run | walk`, `RunScreen` colours and drives off that pair, and the cue
keys in `RunCueText.cs` speak "start running". A circuit needs a segment
that carries its own label and says it.

## Shape

Extend the interval side rather than fork it. The clock, the -10s warning,
the segment-start cue and the pause/resume are already correct and tested.

* A `work | rest` segment kind, plus a per-segment `label` the cue speaks.
  `run | walk` stays exactly as it is, so `0-to-5k.json` is untouched.
* Rounds: a `repeat` count over a block of segments, so a Tabata is eight
  repeats of 20s work + 10s rest rather than sixteen hand-written segments.
* Cue text for the new kinds in `RunCueText.cs`, en and nl, written by hand.
  Say the movement name and the seconds. No dataset supplies this: the
  exercise DB has nothing for body waves, dead arms, golf swings or horse
  stance (see `exercise-db-own-dataset.md`).
* A screen. `RunScreen` is run/walk-shaped and stays that way; a circuit
  screen wants the movement name large, the seconds counting down, and the
  next movement small underneath. Gallery entry, light and dark, per
  AGENTS.md "Done means".
* Session storage reuses what the run screen already writes, so a finished
  circuit counts toward the week contract as its own kind of day.

## First content

`data/programs/mobility-9.json`, nine movements at 60s, ~9 minutes:

    lymphatic hops · body waves · trunk twists · arm swings · dead arms
    golf swings · marches · ballet squats · horse stance

Owner's routine, reviewed 2026-08-31. Known gaps if it is ever revised: no
hip hinge anywhere, and four consecutive rotational upper-body minutes.
Both are content decisions, not engine ones.

* Next step: decide whether `repeat` lands in v1 or the mobility circuit
  ships flat first and Tabata follows. Flat is a smaller diff and gets the
  routine on the phone; `repeat` is the thing that makes the type general.
* Links: `src/Groot.Core/Intervals`, `RunCueText.cs`, `RunScreen.razor`,
  `data/programs/0-to-5k.json` · `exercise-db-own-dataset.md`
