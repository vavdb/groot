---
status: "backlog"
tags: [Backend]
docs: research.md
hook: Pick backend path — Supabase vs own ASP.NET Core API — before building sync
order: 10
---
# Backend path spike

Decide Supabase (auth + Postgres + RLS solved now and later) vs own
ASP.NET Core Minimal API + Postgres/SQLite on a VPS. Leaning Supabase per
research.md §5.4 (owner note), needs a spike to confirm before wiring
multi-user sync.

* Next step: spike Supabase auth (username/password) + RLS against the
  habit-contract schema.
* Links: `research.md` §5.4
