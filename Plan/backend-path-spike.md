---
status: "done"
tags: [Backend]
docs: research.md
hook: decided 2026-08-23: SQLite on device plus our own Groot.Api, no hosted backend
order: 10
---
# Backend path — decided

SQLite on the device as the source of truth, syncing against `Groot.Api`:
our own ASP.NET Core Minimal API, Dapper on SQLite, ASP.NET Identity with
username and password, on the existing VPS behind Caddy. No hosted
backend: no Supabase, no Firebase, no PocketBase.

Endpoints the sync design needs: `POST /auth/register`, `POST /auth/login`,
`POST /sync/push`, `GET /sync/pull?since=`.

* Next step: `sqlite-store-implementation` (the device store) comes first; the
  API is only useful once there are rows to sync.
* Links: `research.md` §5.4, §10 decision log
