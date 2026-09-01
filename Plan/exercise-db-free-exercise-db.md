---
status: "backlog"
tags: [Data]
docs: research.md
hook: Exercise DB seeded from free-exercise-db — public domain, data and stills, 876 records
order: 38
---
# Exercise DB (free-exercise-db)

Feature-matrix MVP row (research.md §3.4). No code yet. The source is
settled (research.md §2.2, reversed 2026-09-01):
[yuhonas/free-exercise-db](https://github.com/yuhonas/free-exercise-db),
876 records, Unlicense, ~97 MB.

What it gives us, checked 2026-09-01:

* Public domain end to end. Data and the two stills per exercise
  (`exercises/<Id>/0.jpg`, `1.jpg`) are the same Unlicense, no media
  exception. Nothing to negotiate before shipping.
* Per record: `id`, `name`, `force`, `level`, `mechanic`, `equipment`,
  `primaryMuscles` / `secondaryMuscles` (17-value enum), `category`,
  `instructions` (array, 4.3 steps average), `images`. English only.
* `category`: strength 584, stretching 123, plyometrics 61, powerlifting
  38, olympic weightlifting 35, strongman 21, cardio 14.
* Combined file at `dist/exercises.json` (~1 MB), or a JSON document per
  exercise under `exercises/`. A `schema.json` to validate against.

Known work:

* Seeding needs `sqlite-store-implementation` landed first, then
  `exercises(id, slug, source)` per the §5 sketch.
* The eight free-string `exercise` ids in `data/programs/gzclp-rack.json`
  resolve against it, except `overhead-press`, which is there under other
  names (`Standing Military Press` and kin) and needs a mapping.
* Vendor the bytes rather than hotlinking raw.githubusercontent. Zenith
  Fits DB died mid-2026 and took 593 videos with it (research.md §2.2).
* No NL instructions, by choice. Groot writes its own copy, the way
  `RunCueText.cs` does.
* Nothing here for mobility work: the 123-entry `stretching` category is
  mostly passive holds and misses most of a circuit. That content is
  hand-authored. See `timed-circuit-program.md`.

* Next step: land `sqlite-store-implementation`, then seed and map.
* Links: research.md §2.2, §10 · `Research/UI/README.md`
