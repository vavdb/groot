---
status: "backlog"
tags: [Auth, Backend]
docs: research.md
hook: MVP auth — username and password only, zero PII, ASP.NET Identity in our own Groot.Api
order: 36
---
# Auth: username/password, zero PII

MVP auth is username and password only, no PII collected. Later: Google
sign-in (Android) and Apple ID (iOS).

The backend is decided (`backend-path-spike`): our own `Groot.Api`, so this
is ASP.NET Identity plus JWT that we write. Two endpoints for the MVP,
`POST /auth/register` and `POST /auth/login`; OpenIddict is the path if
third-party providers ever arrive.

* Links: `research.md` §5, §5.4
