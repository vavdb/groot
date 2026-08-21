---
status: "backlog"
tags: [Monetization, UI]
docs: Research/ads.md
hook: AdSlot component reserving space above bottom nav, flag-gated off, no SDK in MVP
order: 39
---
# Ad slot (reserved space only)

`AdSlot` Razor component in `Groot.UI`, rendered above bottom nav on
Home/Progress only — never on session runner or run screen. Fixed height
so content doesn't jump. MVP: renders nothing, collapses; `FeatureFlags.Ads`
bool. Hard constraint: health data firewall — no health-derived signals
into ad requests, no ad SDK init on screens rendering health data.

* Links: `Research/ads.md`
