# Research: reserving space for ads

*Requested 2026-08-18. Status: research only — no ad SDK in MVP, but the UI reserves the slot now
so adding one later is a config change, not a redesign.*

## 1. What "reserve space" means concretely

- A single `AdSlot` Razor component in `Groot.UI`, rendered above the bottom nav on Home and
  Progress screens only. Never on the session runner or the run screen (mid-workout ads kill the
  product; every competitor that does this gets slaughtered in reviews).
- Fixed height (Google adaptive banner heights land between 50 and 90 dp depending on width) so
  content never jumps when an ad loads or fails.
- MVP behavior: the slot renders nothing and collapses for the owner build. A `FeatureFlags.Ads`
  bool keeps it honest.

## 2. Options

| Route | How | Notes |
|---|---|---|
| Google AdMob (Android/iOS) | community MAUI binding (`Plugin.MauiMTAdmob`) or official Google Mobile Ads SDK via binding libs | banner + native formats; the default choice; verify plugin maintenance state at build time |
| AdSense (web PWA) | script include on `Groot.Web` | blocked by ad-blockers often; low traffic = low revenue |
| Privacy-first networks (EthicalAds, Carbon) | simple JS/api, no tracking | fits the no-PII stance; lower CPM; mainly web |
| No ads: Pro tier instead | one-time unlock or cheap sub via Play/App Store billing | most compatible with the privacy positioning; ads stay a fallback |

## 3. The two constraints that actually matter

1. **Health data firewall.** Google Play policy and the Health Connect terms forbid using health
   data for advertising. Groot reads sleep via Health Connect → ad code and health code must be
   provably separated: no health-derived signals into ad requests, no ad SDK initialization on
   screens that render health data. Document this in the Play data-safety form. This is the
   hardest constraint and it's non-negotiable.
2. **EU consent (GDPR + DMA).** NL users → Google UMP consent flow (or equivalent CMP) before any
   personalized ad. Non-personalized-ads-only mode is the simpler path: no consent wall, lower
   CPM. AdMob supports NPA per request.

## 4. Tension with the product stance

Groot's pitch is "your data, no accounts-harvesting, self-hosted sync." A tracking ad SDK inside
that app contradicts the pitch. If ads ever ship: non-personalized only, banner only, free tier
only, and the Pro unlock removes them. Realistic expectation: hobby-scale DAU makes ad revenue
pocket change; the Pro tier is the more plausible model. The slot reservation costs nothing either
way.

## 5. MVP action list

1. `AdSlot` component with fixed-height placeholder, flag-gated off. (Design: slot drawn in the
   next mockup revision, 56 dp, above bottom nav.)
2. Keep ad SDKs out of `Groot.Core`/`Groot.UI` dependencies — slot is a placeholder, integration
   lives in the heads (`Groot.App`/`Groot.Web`) if it ever happens.
3. Decision deferred to store-release time: AdMob NPA banners vs Pro-tier-only.
