---
status: "backlog"
tags: [Data]
docs: research.md
hook: Exercise DB from exercises-dataset — source decided, MIT data only, media blocked on a Gym visual licence
order: 38
---
# Exercise DB (own dataset)

Feature-matrix MVP row (research.md §3.4): exercise database, own dataset.
No code yet. The source is settled (research.md §2.2): the
[vavdb/exercises-dataset](https://github.com/vavdb/exercises-dataset) fork,
1,324 records, with `yuhonas/free-exercise-db` as a public-domain complement.

What the fork actually gives us, checked 2026-09-01:

* MIT: names, category, body_part, equipment, target, muscle_group,
  secondary_muscles, and instruction text in 10 languages. This is the part
  Groot seeds `exercises(id, slug, source)` from.
* Not ours: `images/` and `videos/` are © Gym visual, redistributed upstream
  under a permission that does not travel with a clone. Ship no GIF or
  thumbnail until Gym visual licences them to Groot; then 180×180 and an
  attribution line, per their terms.
* Nothing for mobility work. It is a gym-equipment database: 29 cardio
  records, zero hits for mobility, warm-up, qigong or tai chi, and its 57
  "stretch" entries are passive holds. A mobility circuit is hand-authored
  content, not a lookup. See `timed-circuit-program.md`.

* Next step: land `sqlite-store-implementation` first, then seed an
  `exercises` table from the MIT half and resolve the eight free-string
  `exercise` ids in `data/programs/gzclp-rack.json` against it.
* Links: research.md §2.2, §10 · `Research/UI/README.md`
