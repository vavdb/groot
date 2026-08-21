---
status: "backlog"
tags: [Data, Import]
docs: data/log.csv
hook: Import Strong-format CSV export into the Groot data model
order: 37
---
# CSV import (Strong format)

Feature-matrix MVP row, no code yet. `data/log.csv` is the real sample —
Strong's columns cover lift sets cleanly but have no duration/distance
field for runs (see `manual-run-entries-and-notes`). Import needs to
tolerate that gap, not just parse the happy path.

* Links: `data/log.csv`, `manual-run-entries-and-notes`,
  `data-persistence-layer`
