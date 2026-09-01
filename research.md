# Groot — Research Document

*Working title: **Groot** ("big" in Dutch — also: a tree that grows).*
*Date: 2026-08-18. Author: research pass by Claude for Vincent.*

---

## 1. Project summary

A workout logger / personal training app in the StrongLifts tradition: barbell-program-first, fast set logging, rest timers in the notification area, automatic progression. Targets:

| Platform | Form |
|---|---|
| Web | Blazor WebAssembly PWA |
| Android | Native app (store or sideload), notification-area rest timers |
| iOS | Native app (MacBook available for build/test) |

Multi-user, remote sync, username-only accounts for MVP (no PII), later Google sign-in (Android) / Apple ID (iOS). Free-or-cheap backend. Google Health (= Health Connect) sync desired; Apple Health equivalent on iOS.

---

## 2. What the user already has

### 2.1 Training history export (`History7327682813780755646.csv`)

Strong-app-compatible CSV format:

```
Date, Body Weight (KG), Body Weight (LB), Workout Name, Exercise Name, Set Order, Weight (KG), Weight (LB), Reps, Notes
```

- **1,622 set rows, 86 distinct workout days, Nov 2020 → Feb 2025** (gaps in 2023).
- Dutch locale dates (`wo 11 nov 2020`) — import parser must handle `nl-NL` day/month abbreviations.
- **7 programs logged:** GZCLP (375 sets), GreySkull LP with Arms (357), nSuns 531 LP 6-day deadlift (266), nSuns 531 LP 5-day (178), lvysaur 4-4-8 (112), GZCLP-CGB (11), and a self-made program **"Vin1"** (323 sets).
- 36 distinct exercises; top: Bench Press (231), Overhead Press (208), Deadlift (192), Squat (188).
- Logged maxes: Squat 190 kg, Bench 87.5 kg, Deadlift 110 kg, OHP 57.5 kg. Bodyweight ~95–96 kg where recorded.
- Quirks the importer must survive: empty `Reps` cells, `0.0` weights (push-ups), **negative weights** (assisted chin-ups, −35 kg), `Set Order` restarting per exercise, warm-up ramp sets mixed with work sets, farmer's walks logged as reps=1.

**Consequences for Groot:**
1. CSV import (Strong format) is a must-have — it onboards the user's own 5 years of history and anyone leaving Strong.
2. Data model needs: negative/zero weights, bodyweight-per-day, per-program history, notes per set.
3. User builds own programs ("Vin1", GZCLP-CGB variant) → **program editor is core**, not just canned programs.

### 2.2 Exercise media dataset

Fork: [vavdb/exercises-dataset](https://github.com/vavdb/exercises-dataset) (fork of `hasaneyldrm/exercises-dataset`, synced Jul 2026).

- **1,324 exercises**. **The data is MIT; the media is not** (checked 2026-09-01, see below).
- Animation **GIFs + 180×180 thumbnails stored inside the repo** (~128 MB). The bytes are there and survive any hosting death (relevant: Zenith Fits DB died mid-2026; its 593 videos are unrecoverable — site 410, R2 bucket 401). **The licence does not come with them.**
- **Media licence, the blocking bit.** `images/` and `videos/` are © Gym visual (https://gymvisual.com/), redistributed in that repo under a written permission the upstream author obtained. The fork's `LICENSE` says it plainly: cloning grants no rights to the media, and a project that wants it obtains its own licence from Gym visual. Terms: 180×180 only, attribution "© Gym visual — https://gymvisual.com/" on every use. **Groot has no such permission today.** Ship the data now, the GIFs only after Gym visual licences them to us or the stills come from elsewhere.
- MIT does cover names, categories, body parts, equipment, targets, muscle groups and **every instruction string** — the parts Groot needs first.
- Muscle-group + equipment metadata, step instructions in **10 languages** (`en, es, fr, hi, it, ko, pl, ru, tr, zh`). **No Dutch**, and none is wanted: Groot writes its own NL copy, the way `RunCueText.cs` already does.
- Complement: [yuhonas/free-exercise-db](https://github.com/yuhonas/free-exercise-db) — 800+ exercises, public domain, static start/end photos, good fallback stills where a GIF is overkill.

---

## 3. Competitor / prior-art research

### 3.1 StrongLifts 5x5 app (the "felt nice" benchmark)

What makes it feel nice — these are the UX bars Groot must clear:

- **One-tap set logging**: big circles, tap = done at target reps; extra taps count down reps; tap-hold to adjust weight/reps.
- **Rest timer auto-starts** after logging a set; suggested rest adapts (shorter after easy sets, longer after failed sets).
- **Notification-area timer**: countdown lives in the notification shade; **you can log the next set from the notification** without unlocking the phone. This is the single stickiest feature.
- **Auto progression**: +2.5 kg (configurable, fractional plates supported) when all sets pass; **auto deload** (e.g. −15 %) after repeated fails.
- **Plate calculator** per side, configurable bars (curl bar → SSB).
- Apple Health / Activity ring sync; cross-device sync iOS↔Android.
- Programs: 5×5 variants + Madcow. Freemium: core free, Pro subscription for assistance work/programs.

### 3.2 Personal Training Coach (Aptoide APK; the second app used)

- 100K+ downloads, rating ~3/5, Android 7.1+.
- Program library overlaps the user's history: StrongLifts 5x5, **GreySkull LP**, **Wendler 5/3/1**, PPL — plus **custom routine builder** with custom exercises.
- Auto-increment weight, **RPE tracking**, warm-up routine generator, progress graphs, dark/light themes, cloud backup, metric+imperial.
- Take-away: this app ≈ feature superset of StrongLifts minus polish. Groot's opportunity = StrongLifts polish × PTC flexibility (custom programs, RPE) — which matches the user's actual history (5 different LP/531 programs + self-made ones).

### 3.3 Garage Gym Reviews — "Best Personal Training Apps" roundup

Their picks are mostly **coaching subscriptions**, not loggers: Future ($199/mo human coach), Caliber (free tier + $19+/mo), JuggernautAI ($35/mo AI powerlifting periodization), SHRED ($9.99/mo), iFIT/Peloton/Centr (classes), Train Hard (CrossFit).

Their judging criteria worth stealing as Groot quality bars: individualized progression, unlimited plan modification, automatic logging, demonstrated progress mechanics, price honesty.

**Positioning conclusion:** the market splits into (a) $10–200/mo coaching subscriptions and (b) one-time/freemium self-directed loggers (StrongLifts, Strong, Hevy, Boostcamp). Groot competes in (b): self-owned data, no subscription, program-agnostic LP/531/GZCL engine. The roundup confirms nobody in (a) serves a lifter who writes their own "Vin1" program — that's (b) territory and it's where the user already lives.

### 3.4 Feature matrix → MVP cut

| Feature | StrongLifts | PTC | **Groot MVP** | Groot later |
|---|---|---|---|---|
| One-tap set logging | ✅ | ➖ | ✅ | |
| Rest timer in notification + log-from-notification | ✅ | ➖ | ✅ (Android) | iOS Live Activity |
| Auto progression / deload rules | ✅ fixed | ✅ basic | ✅ rule-per-lift (LP) | 531/GZCL cycle engines |
| Program editor (own programs) | ➖ limited | ✅ | ✅ | share programs |
| Plate calculator | ✅ | ❓ | ✅ | |
| Exercise DB with animations | ➖ | ✅ static | ✅ (own dataset) | |
| CSV import (Strong format) | ➖ | ➖ | ✅ | export too |
| Graphs (e1RM, volume, bodyweight) | ✅ | ✅ | ✅ basic | tree-growth viz |
| Multi-device sync | ✅ paid | ✅ | ✅ | |
| Health Connect / Apple Health | Apple only | ➖ | ➖ | ✅ phase 2 |
| RPE | ➖ | ✅ | ➖ | ✅ |

---

## 4. Tech stack

### 4.1 Direct answer: is Blazor/WASM good?

**Blazor yes — but WASM alone can't deliver the Android notification timer.** A PWA cannot own a foreground service, cannot post a chronometer countdown notification, and gets throttled by Doze. The notification-area timer (your stickiest requirement) is native-only territory.

**Recommendation: .NET MAUI Blazor Hybrid + Blazor WASM sharing one Razor Class Library.** One C# codebase, ~90 % shared UI, full native API access where it matters:

```
Groot.sln
├─ Groot.Core            // domain: programs, progression rules, e1RM, plate math (pure C#)
├─ Groot.UI              // Razor Class Library: ALL pages/components, CSS
├─ Groot.Web             // Blazor WASM PWA head (hosts Groot.UI)
├─ Groot.App             // .NET MAUI Blazor Hybrid head (Android + iOS + Mac Catalyst)
│   ├─ Platforms/Android // foreground service, chronometer notification, Health Connect
│   └─ Platforms/iOS     // HealthKit, local notifications, (later) Live Activity
├─ Groot.Sync            // sync client: SQLite local store + remote push/pull
└─ Groot.Api (optional)  // ASP.NET Core Minimal API if self-hosting backend
```

- .NET 10 (LTS, Nov 2025) — MAUI and Blazor both current and stable on it.
- UI runs in `BlazorWebView` on device: same components as the web app, but C# executes natively (no WASM sandbox) → direct calls into Android/iOS APIs from DI-injected platform services.
- MacBook: required + sufficient for iOS builds (MAUI remote build from Windows also works, but building on the Mac directly is less friction).
- Testing/dev loop: web head for fast iteration, device for native features.

**Alternatives considered (short):**

| Stack | Verdict |
|---|---|
| Blazor WASM PWA only | ❌ no notification timers, no Health Connect, iOS PWA install friction |
| Uno Platform | C# everywhere incl. WASM from one head; smaller ecosystem, less Blazor skill reuse |
| Flutter | Best-in-class mobile polish; but Dart, discards your C# advantage |
| Kotlin Multiplatform | Native maximalism, two UI stacks anyway; wrong fit |
| Avalonia | Desktop-first; mobile+web immature for this |

### 4.2 Android notification-area rest timer (the hard requirement, solved)

Implementation in `Platforms/Android` (all reachable from C# via Mono.Android bindings, no plugins needed):

1. **Foreground service** (`dataSync`/`specialUse` type) started when a rest timer begins; keeps the process alive through Doze.
2. Notification built with **`SetUsesChronometer(true)` + `SetChronometerCountDown(true)` + `SetWhen(now + remainingMs)`** → the OS renders a live mm:ss countdown in the shade with **zero notification updates** (battery-friendly, no throttling).
3. **Notification actions**: "✓ Done set" / "+30 s" / "Skip" buttons → `PendingIntent` → log next set without unlocking (StrongLifts parity).
4. End-of-rest alert: `AlarmManager.SetExactAndAllowWhileIdle` (declare `USE_EXACT_ALARM` — permitted for timer functionality) firing sound/vibration.
5. Android 16 "Live Updates" (`ProgressStyle` notifications) as a progressive enhancement.
6. Caveat to test early: OEM battery killers (Samsung/Xiaomi) — foreground service + exact alarm is the correct combination, but test on real hardware.

**iOS equivalent:** no persistent countdown notifications exist. MVP: schedule a `UNUserNotificationCenter` local notification at timer end ("Rest over — Squat set 3"). Later: **Live Activity** (lock screen + Dynamic Island countdown, iOS 16.1+) — requires a native widget extension, which is awkward (not impossible) from MAUI; park it behind a flag.

### 4.3 Timers, background, web head

Web PWA gets a best-effort timer only (tab notification + sound if open; Web Push exists but is unreliable for second-precision) — acceptable: the phone is the gym device, web is for program editing + stats couch-review.

---

## 5. Storage / backend / sync

### 5.1 Requirements recap

Multi-user; free or cheap; sync web+Android+iOS; MVP auth = username(+password) only, zero PII; later Google (Android) & Apple ID (iOS) sign-in; user stats only.

### 5.2 Architecture principle: **local-first**

Gyms are concrete basements. The app must be 100 % functional offline:

- **SQLite on device** (EF Core or sqlite-net) = source of truth for the session.
- Sync = background push/pull of append-mostly rows (workouts, sets) with GUID PKs, `updated_at`, soft-delete tombstones, last-write-wins. Trivial conflict surface: one user rarely edits the same set on two devices simultaneously.
- Web head: same model against IndexedDB (via `Microsoft.Data.Sqlite` WASM or just call the API online-only for MVP — web offline can wait).

### 5.3 Backend options compared

| Option | Free tier | Auth (username now, Google/Apple later) | C# story | Risks |
|---|---|---|---|---|
| **Supabase** (Postgres + auth + REST) | 500 MB DB, 50k MAU — ample | ✅ built-in: anonymous, email/password, Google, Apple | `supabase-csharp` community client, works in MAUI + WASM | free projects **pause after ~1 week inactivity** (cron-ping or accept cold start); community-maintained C# lib |
| Firebase (Firestore + Auth) | Generous reads/day | ✅ all providers | ❌ weak client SDK for C#; REST workarounds | Google API churn; NoSQL model fights relational workout data |
| **PocketBase** on VPS | n/a — Hetzner CX22 ≈ €4/mo | ✅ built-in users + OAuth providers | REST/realtime, thin C# wrappers | you run it (it's a single Go binary + SQLite; near-zero ops) |
| Own ASP.NET Core Minimal API + Postgres/SQLite on VPS | ≈ €4/mo | roll your own (ASP.NET Identity) — OAuth wiring is on you | ✅ 100 % your stack | you build auth, sync endpoints, backups — fun but slower to MVP |
| Azure (App Service free + Azure SQL free 32 GB) | genuinely free tiers exist | Entra External ID / roll your own | ✅ native | free tiers throttle/idle; pricing cliffs later |

### 5.4 Decision (2026-08-23, owner)

**SQLite on the device, and our own API server. No Supabase, no Firebase, no PocketBase.**

The device keeps SQLite as the source of truth (§5.2) and syncs against `Groot.Api`, an ASP.NET
Core Minimal API we write and run on the existing VPS. Auth is ASP.NET Identity with
username and password, no PII (`Plan/auth-username-password.md`). The table above stays as the
record of what was compared; the rows for hosted backends are history now, not options.

What this costs, honestly: the one to two weeks of auth and sync plumbing a hosted backend would
have handed over. What it buys: one stack end to end, no third-party account in the critical path,
no free tier that pauses after a week of quiet, and a schema that is ours to change.

Data model sketch (works on either):

```
users(id, username, created_at)                       -- no PII
programs(id, user_id, name, json_definition, ...)     -- progression rules as data
workouts(id, user_id, program_id, date, bodyweight_kg, notes, updated_at, deleted)
sets(id, workout_id, exercise_id, set_order, weight_kg, reps, rpe?, is_warmup, updated_at, deleted)
exercises(id, slug, source)                           -- seeded from exercises-dataset
```

---

## 6. Health sync

### 6.1 Android — Health Connect (NOT Google Fit) — refreshed 2026-08-18

**Landscape (verified Aug 2026):**
- **Google Fit APIs: EOL end of 2026** (signups closed May 2024). Build nothing against them.
- **Fitbit Web API: hard shutdown September 2026.** Its cloud successor is the **new "Google Health API"** — a rebuilt REST surface (31 data types incl. sleep-with-stages, 4 read methods). Every scope is **Restricted** → mandatory privacy/security review with a growing queue. **Groot does NOT need it** — that API is for server-side pulls of Fitbit-account data; Groot reads on-device.
- **Health Connect = the right integration**, and it matured: **Jetpack SDK v1.1.0 stable since Nov 2025** (`androidx.health.connect:connect-client`). Built into Android 14+; Android 9–13 via the Health Connect app from Play. On-device store, per-data-type permissions, aggregates Fitbit/Pixel Watch/Samsung Health/Garmin — whatever writes sleep on the phone, Groot can read locally, no cloud, no Restricted-scope review.

**What v1.1.0 adds that Groot actually wants:**
- **Background reads** (`READ_HEALTH_DATA_IN_BACKGROUND`, user-grantable) → morning notification "6h12 sleep · heavy squat day — adjust?" without the app being opened.
- **History reads** (`READ_HEALTH_DATA_HISTORY`) → onboarding backfill: pull months of sleep/weight history on day one instead of starting empty.
- Sleep sessions **with stages** (light/deep/REM) → recovery context can be stage-aware later.
- New record types (skin temperature, training plans, exercise routes, FHIR medical records) — noted, not needed.

**Groot's permission set — IN MVP (owner decision 2026-08-18, after confirming HC is fully 2-way):**
`WRITE_EXERCISE` (sessions out) + `READ_SLEEP` (+ optionally `READ_WEIGHT`, `READ_STEPS`) + the
background/history grants above. Each permission = separate user consent. The Play Console health
declarations only bite at store release; sideloading during development defers that paperwork.
MAUI route unchanged: AndroidX binding NuGets track androidx (`Xamarin.AndroidX.Health.Connect.*`),
Kotlin-coroutine interop glue expected. **GPS: skipped for MVP entirely (owner decision 2026-08-18);
GPS-lite (distance/pace, coordinates discarded) parked at MVP+1 — see habit-system.md §3.1.**

### 6.2 iOS — HealthKit

- First-class in MAUI iOS (bindings are complete): write `HKWorkout` (traditionalStrengthTraining) + samples. Entitlement + usage strings, test on device.
- Ships when the iOS head ships; mirrors the Health Connect integration (which is now MVP on Android).

---

## 7. UI direction

### 7.1 Your style (vincability.com analysis)

Two poles on the same site:

- **vincability.com root** — Swiss-consulting minimal: neutral palette, whitespace, numbered sections (§01/§02), text-forward, zero sales gloss. "See what others miss."
- **/vindicator/** — dark navy/black, **gold/matte-yellow accents**, iridescent highlights, all-caps command-line headers (`SYS // VINDICATOR`), isometric renders, cryptic restrained copy ("Wake up. The gates are dark. Fold anyway.").

Common DNA: **dark-capable, high contrast, technical/typographic identity, restraint over decoration, monospace/console accents.** This DNA should survive into whichever mockup style wins.

### 7.2 Mockup directions (all delivered light + dark)

1. **Neo Cyberpunk** *(requested)* — near-black blue/purple ground, neon magenta/cyan accents, glow edges, scanline texture, monospace numerals. Risk to manage: legibility under gym lighting; keep neon for accents, not text.
2. **Cartoonish Pastel** *(requested)* — rounded geometry, cream/pastel palette, chunky buttons, playful mascot potential (a little tree that flexes). Opposite pole of your usual taste — useful as a contrast test.
3. **Command Console** *(the Vindicator-derivative — predicted winner)* — navy/black + gold, `SYS // GROOT` headers, tabular monospace numbers, one-tap set circles rendered as instrument dials/status LEDs. Groot as cockpit.
4. **Growth Rings** *(the "other idea")* — Groot = tree: deep forest green/bark brown/moss, and a signature visualization: **lifetime tonnage drawn as tree rings; PRs sprout branches**. Organic-minimal, Swiss layout under it. Unique identity nobody in the space has.

Direction 3 or 4 is the bet for "clear but has its own identity"; 1 and 2 calibrate the extremes.

---

## 8. Open questions

1. Build `Groot.Api` and the device store: SQLite plus our own Minimal API (§5.4, decided).
2. iOS Live Activity: worth the native-extension pain, or is end-of-rest notification enough?
3. Program engine scope for MVP: LP rules only (covers GZCLP/GreySkull/StrongLifts), or 531-style cycles too (nSuns needs them)?
4. App distribution: Play Store + TestFlight, or sideload/APK for personal circle first?
5. Web offline (IndexedDB) — MVP or later?

## 9. Suggested build order

1. `Groot.Core`: program/progression engine + plate calc + e1RM (pure C#, unit-tested, fun part).
2. `Groot.UI` + web head: program editor, workout runner, CSV import of your own history.
3. MAUI head: Android first — foreground-service chronometer timer + notification actions (the moat).
4. Sync against `Groot.Api` (GUIDs + updated_at + tombstones).
5. iOS build on the MacBook; TestFlight.
6. Phase 2: Health Connect + HealthKit writes, RPE, 531 cycles, tree-ring viz.

---

## 10. Decision log

**2026-08-18 — UI direction decided: Growth Rings.**
- Winner: Growth Rings (§7.2 #4). Live mockup: `design/growth-rings.html` (v2 — English, light+dark side by side).
- Language: **English default UI, translations first-class from day 1** (en + nl minimum). All strings via resource keys (`IStringLocalizer`/.resx or JSON), locale drives number/date/unit formatting — nothing hard-coded in components. v1 Dutch render kept as the nl reference.
- Rejected directions archived with provenance + IP notes in `Research/UI/` (README.md documents the copyright position: hand-authored code, OFL fonts, MIT/public-domain exercise media, methodology-name trademark cautions).
- **Naming guardrail**: identity stays abstract rings, never a character or mascot; trademark search (EUIPO/Benelux) before any commercial launch.

**2026-08-18 (later) — stack confirmed, backend decided, habit system designed.**
- Stack **confirmed by owner: .NET MAUI / Blazor** (per §4 layout).
- Backend: **self-hosted on the existing Linux VPS**. Shape settled on 2026-08-23 (§5.4): `Groot.Api`, our own ASP.NET Core Minimal API over SQLite, behind Caddy with Let's Encrypt, a systemd unit, and nightly backups via Litestream or restic. Apple sign-in, if it ever arrives, needs the $99/yr Apple developer account either way, which iOS distribution requires regardless.
- **Habit system designed** — weekly contract (2×lift + 2×run + 1×rest), 2 jokers/week, week-streaks not day-streaks, per-side weight entry, 0→5K interval runner with audio cues. Full spec: `design/habit-system.md`; mockup: `design/habit-rings.html`. Viz decision: **contract card + GitHub-style season grid on Home (MVP), rings become the lifetime view** — grid for weeks, rings for years.

**2026-08-18 (later still) — MVP scope pinned.**
- **MVP programs** (catalog: `Research/programs.md`): **GZCLP (rack edition)** — owner's profile: newbie-gains powerlifting LP for barbell + dumbbells + power rack (chin-ups/DB rows replace machine work) — and **0→5K**. Custom builder remains core; other programs staged v1.1+.
- **TTS voice cues in MVP**: cue points on interval segments (`{at, key, args}`), text through the i18n pipeline, spoken by platform TTS (Android `TextToSpeech` with audio-focus ducking; iOS `AVSpeechSynthesizer` with `DuckOthers`). Offline, free, locale-matched. Implementation sketch in `design/habit-system.md` §3.2.
- **Gemini free tier ships in MVP** (§11 architecture: `IAdvisor`, BYOK, off until key entered).
- **Health Connect moves up, including READ**: owner tracks sleep in Google's ecosystem. Read `SleepSessionRecord` (+ optionally `WeightRecord`, `StepsRecord`) for recovery context; write workouts as before. Read effort ≈ write effort (same binding, extra permission declarations). Slot: right after MVP core loop works — "MVP+1", not phase 2. Details §6.1.
- **Identity DECIDED (2026-08-18): name "Groot" stays, plant/tree language removed.** Neutral variant canonical (`design/habit-rings.html`); plant variant + all rejected directions archived in `Research/UI/`. Rename candidates (Stam/Eik/Kernhout/Jaarring) documented in habit-system.md §6b as considered-and-rejected. Palette/type/rings stay; zero botanical wording.
- **Week start is a user setting, not ISO**: default from locale `FirstDayOfWeek` (Mon EU, Sun US), overridable; contract math/grid/`weeks` table all key off it (spec §1.1 rule 1 updated; table keyed by `week_start_date`).
- **Health Connect promoted into MVP** (owner decision 2026-08-18, after confirming it's fully 2-way): write workouts + read sleep, incl. background/history grants. Play paperwork deferred by sideloading until store release. GPS stays out of MVP.
- **Copy voice rule added** (habit-system.md §5b): user-facing strings pass the humanizer checklist — the first mockups didn't (negative parallelisms, aphorisms, em dashes in cues), owner caught it.
- **Backend REVERSED (2026-08-18, post-scaffold): own `Groot.Api` instead of PocketBase.** The PB recommendation predated the local-first sync design. What the backend really does is username auth + three sync endpoints; the sync protocol (deltas, tombstones, LWW) is hand-built either way, and PB customization means JavaScript hooks while the whole stack is C#. Own API wins on: shared DTOs with Groot.Core, integration tests inside the sln, one language in an Apache-2.0 repo, same ops weight (dotnet publish + systemd). PB's OAuth advantage is deferred anyway (MVP = username-only; OpenIddict later). Shape: Minimal API, Dapper on SQLite via Groot.Data, JWT, `POST /auth/register|login`, `POST /sync/push`, `GET /sync/pull?since=`. Deploy files (Caddy snippet, systemd unit, backup script) land with Groot.Api, written against the real ports and paths.

---

## 11. Optional AI integration (free-first)

Use cases ranked by value/effort: (1) weekly ring-close recap text ("W14: +2.5 kg squat, jokered Thursday, 3rd week above 4 t volume"); (2) natural-language quick log ("squat five by three at hundred" → structured sets); (3) program tweak advice ("failed OHP 3rd week running — deload options"); (4) full plan generation from constraints. Skip form-check video analysis — different league.

**Architecture: BYOK (bring your own key), optional, off by default.** `IAdvisor` interface in Core; any OpenAI-compatible endpoint configurable. Zero server cost, zero liability, app fully functional without it. Only anonymized training numbers leave the device — never account/identity data (fits the no-PII stance).

| Option | Cost | Notes |
|---|---|---|
| **Google Gemini API (Flash)** | genuinely free tier | best free default for BYOK; generous daily quota |
| Groq (Llama/Qwen) | free tier | very fast, fine for recap/parse tasks |
| Mistral La Plateforme | free tier | EU-hosted option (data locality nice-to-have) |
| OpenRouter `:free` models | free | one endpoint, many models; availability varies |
| **On-device**: Apple Foundation Models (iOS 26+) / Android AICore·Gemini Nano | free forever, private, offline | the end-state for recap + NL-parse; MAUI bindings = some friction; phase 3 |
| Ollama on the VPS | free (your hardware) | the VPS is CPU-only → slow tokens; OK for async weekly recaps generated server-side overnight |
| Anthropic Claude API | no free tier (Haiku is cheap) | honest note; quality option if ever paying |

Recommendation: ship `IAdvisor` + BYOK with Gemini Flash as documented default; evaluate on-device once the MAUI bindings mature. The weekly recap can also be a pure-code template (no AI) for v1 — AI upgrades the prose, not the feature.

## 12. Program library — can we ship StrongLifts/GZCLP/nSuns?

Legal shape (method vs. expression vs. name):

- **Training methodologies are not copyrightable** (systems/ideas/facts — US & EU doctrine agree). Sets, reps, percentages, progression rules: free to implement from scratch. What's protected: the authors' *prose*, spreadsheets' creative layout, app assets (never copy), and **names as trademarks**.
- Precedent: open-source **Liftosaur** and commercial **Boostcamp** ship GZCLP/nSuns/GSLP variants publicly for years. Use Liftosaur only as a cross-check reference — it's AGPL; don't lift code or definition files into a non-AGPL app. Implement from public descriptions (r/Fitness wiki *numbers*, authors' free posts), write all prose ourselves.

Per program:

| Program | Method status | Name risk | Ship as |
|---|---|---|---|
| GZCLP | Cody Lefever published free, encourages spread | low — community name | **"GZCLP"** + credit "based on the GZCL method by Cody Lefever" + link |
| nSuns 531 LP | free reddit spreadsheets; derived from Wendler 5/3/1 | "5/3/1" is Wendler's brand; "nSuns" community handle | **"nSuns LP"**, describe as "531-style progression", credit + link |
| lvysaur 4-4-8 | free reddit post | low | **"lvysaur 4-4-8"** + credit |
| StrongLifts 5×5 | 5×5 LP is ancient (Reg Park, 1960s) | **"StrongLifts" firmly trademarked** | **"Classic 5×5 LP"** — identical mechanics, never the SL name/logo/copy |
| GreySkull LP | AMRAP-LP mechanics free to implement | **"Greyskull" trademarked** (John Sheaffer) | **"AMRAP LP (GSLP-style)"** or ship Phrak's variant ("Phrak's LP", community-released) |
| 0→5K running | interval plan = generic public knowledge | "Couch to 5K/C25K" trademarked in some jurisdictions (NHS et al.) | **"0→5K"**, "couch-to-5K style" only descriptively |

House rules: attribution screen in-app ("Programs" → "About these methods") with links to every author; all instruction text self-written or from the MIT-licensed half of the exercise dataset (§2.2); **no exercise GIFs or thumbnails until Gym visual licences them to us** (§2.2, the media is not ours to ship); no screenshots/assets/audio from any app. This + `Research/UI/README.md` = the complete copyright position.

## 13. Stack details (owner questions, 2026-08-18)

### 13.1 MudBlazor — DECIDED (owner overruled, 2026-08-18): framework for the screens

First take here said "custom core screens"; owner ruled otherwise: main screens are built with a
UI framework, hand-rolling is out. Analysis of where MudBlazor will bite (v9.8.0, Aug 2026,
actively released; works in `BlazorWebView`):

**Expected friction, ranked:**
1. **No bottom navigation component** — proposal [#2206] open for years. Groot's primary nav is
   4 bottom tabs. Options: `MudTabs Position="Bottom"` (tab semantics, workable) or a 10-line
   `MudPaper` + icon buttons strip. Small, but it's the first thing we build.
2. **No long-press gesture** — tap=done / hold=edit on set circles needs own pointer-event JS
   interop regardless of framework.
3. **Identity visuals stay custom** — season grid, year rings (SVG), set circles. MudChart covers
   the e1RM line chart, not heatmaps/radials. This was always going to be custom; it's the
   identity, not the chrome.
4. **De-Materializing the theme** — moss/bark/amber via `PaletteLight`/`PaletteDark`,
   Fraunces/Public Sans via `Typography`, ripple off, elevation down, radii via `LayoutProperties`.
   Budget 1–2 days; Material leaks through in focus states and density defaults.
5. **WASM payload** on the web head grows (lib + icon font); PWA caching absorbs it after first
   load. Measure, don't guess.
6. **iOS Hybrid details**: safe-area insets are ours; test dialogs + keyboard on device. No known
   AOT blockers (managed components, no dynamic codegen — unlike EF Core).
7. **Release cadence**: v7→v8→v9 in about two years, breaking changes each major. Pin the major,
   upgrade deliberately.

**What it buys for free:** program-editor forms + validation, dialogs, snackbar (PR toasts),
history table/DataGrid, pickers, drawer/appbar, dark/light theme switching, localization plumbing.

**Docs tooling:** no MudBlazor skill/MCP in this environment or the org registry, but
[MudMCP](https://github.com/mcbodge/MudMCP) exists — an unofficial MCP server (Roslyn-parsed docs
for ~85 components, 12 tools, stdio transport). Add it when scaffolding starts:
`claude mcp add` with the stdio command from its README. Not affiliated with the MudBlazor team;
treat output as docs lookup, verify against mudblazor.com.

**Alternatives weighed (owner widened the question to "any framework, just not homegrown"):**
Fluent UI Blazor (Windows design language — wrong feel on Android), Radzen (neutral look, good
CSS-var theming, smaller community — the runner-up if Material must go), Syncfusion (heavy,
community-license revenue terms to check against an Apache-2.0 repo), Blazorise (extra provider
abstraction + commercial tier), Ant Design Blazor (fine, adds nothing over Mud).

**Confirmed: MudBlazor — with a reframe that shrinks friction #4.** Material was framed as the
enemy; wrong frame. Material is Android's native design language and Groot is Android-first:
adopt Material structure (touch targets, motion, elevation logic), express identity through
palette + Fraunces + the three custom visuals. Accent theming instead of a fight; the 1–2 day
estimate drops to roughly half. Radzen noted as fallback; component APIs are close enough that an
early switch is cheap while identity visuals stay custom.

[#2206]: https://github.com/MudBlazor/MudBlazor/issues/2206

### 13.2 CUPID instead of SOLID — what it means for Groot concretely

- **Composable**: `Groot.Core` has zero framework dependencies; small pure classes
  (`ContractEvaluator`, `IntervalEngine`, `PlateSolver`, `ProgressionEngine`) that compose.
- **Unix philosophy**: each does one thing. No `WorkoutService` that knows everything.
- **Predictable**: pure functions, same input → same output. The progression engine and contract
  evaluator are deterministic; unit tests read as the spec.
- **Idiomatic**: modern C# — records, pattern matching, `nullable enable`, no abstraction lasagna.
- **Domain-based**: names and folders from the lifting domain (`Set`, `WeekContract`, `Joker`,
  `CountsAs`), not technical layers (`Managers/`, `Helpers/`).

### 13.3 Composition over inheritance in C# (the Groot version)

No `abstract class ProgramBase` with subclasses per program. Behavior is data plus composed rules:

```csharp
public interface IProgressionRule
{
    ProgressionDecision Evaluate(ExerciseState state, SessionResult result);
}

public sealed record LinearIncrement(decimal Kg) : IProgressionRule { /* +2.5 on success */ }
public sealed record FailLadder(string[] Stages, decimal ResetPct) : IProgressionRule { /* 5x3→6x2→10x1→reset */ }
public sealed record AmrapThreshold(int TotalReps, decimal Kg) : IProgressionRule { /* T3: ≥25 → +weight */ }

public sealed record ProgramExercise(
    ExerciseId Exercise,
    int Tier,
    IReadOnlyList<IProgressionRule> Rules);   // GZCLP T1 = [LinearIncrement, FailLadder]
```

The JSON program definitions map 1:1 onto these records; a new program is new data, not a new
class hierarchy. Same pattern everywhere: `Equipment { unit, achievable_loads[] }` composes a
`PlateSolver` or a `StepList`; cue points compose onto segments. Interfaces + records + DI —
inheritance reserved for actual is-a cases (rare).

### 13.4 Future folders

```
P:\Groot\
├─ src/
│  ├─ Groot.Core/            # domain, zero deps
│  │   ├─ Programs/ Sessions/ Contract/ Equipment/ Intervals/ Import/
│  ├─ Groot.UI/              # Razor Class Library: all screens/components
│  ├─ Groot.Web/             # Blazor WASM head (PWA)
│  ├─ Groot.App/             # MAUI head (Platforms/Android, Platforms/iOS)
│  ├─ Groot.Data/            # SQLite + Dapper, migrations/*.sql, sync client
│  └─ Groot.Api/             # optional, later, on the VPS
├─ tests/Groot.Core.Tests/
├─ data/programs/            # gzclp-rack.json, 0-to-5k.json → embedded resources at build
└─ design/  Research/  docs/
```

### 13.5 Dapper vs EF Core (researched)

Findings (Aug 2026): EF Core carries `RequiresDynamicCode`/`RequiresUnreferencedCode` through its
API — reflection + dynamic codegen for change tracking and query compilation. On MAUI iOS (AOT,
no JIT) that ranges from fragile to blocked; known crash issues on iOS. **Dapper.AOT is actively
maintained (v1.0.52, May 2026)**: source-generated mapping, no runtime reflection, AOT/trimming
clean.

Decision: **Dapper (+ Dapper.AOT) on device.** Fits CUPID: explicit SQL is predictable and
composable, no ORM magic. Schema is ~8 small tables; migrations = numbered `.sql` scripts applied
via `PRAGMA user_version` (a 30-line migrator, unit-testable). `sqlite-net-pcl` noted as fallback.
EF Core only ever server-side (`Groot.Api` + Postgres someday), where AOT doesn't apply.

### 13.6a Windows + web hosting (no Electron)

Electron is a Chromium+Node shell — the wrong tool for C#. The stack already covers every surface:

- **Windows**: `Groot.App` gets a `net10.0-windows` target for free — MAUI on Windows = WinUI 3
  + the same `BlazorWebView` (rendered by WebView2, which ships with Windows). That IS the C#
  equivalent of Electron, minus the bundled browser and the second runtime. Notification timers on
  Windows ride Windows App SDK notifications (nice-to-have, not MVP).
- **Web**: `Groot.Web` (Blazor WASM PWA) is static files — host on the VPS behind Caddy
  (gzip + cache headers, done). PWA install gives the desktop-app feel
  on any OS. No server-side Blazor, no SignalR to babysit.
- **macOS (dev bonus)**: Mac Catalyst target exists in MAUI; useful for testing on the MacBook,
  not a shipping priority.

### 13.6b .NET Aspire? No (owner asked, 2026-08-18)

Aspire orchestrates multi-service systems: service discovery, container resources, dev dashboard,
azd-style deploys. Groot's server side is one Minimal API + static WASM files + SQLite behind
Caddy, deployed by rsync + systemd — nothing to orchestrate, and the MAUI client sits outside
Aspire anyway. Adding it costs an AppHost + ServiceDefaults + a fast-moving SDK dependency for a
dashboard over a single process. If Groot ever grows to several backend services with container
dependencies, revisit. Tracing, if ever wanted, is a few lines of plain OpenTelemetry in Groot.Api.

### 13.6 Program distribution

MVP: `data/programs/*.json` compiled in as embedded resources. **MVP++: downloadable program
definitions from the VPS** — a `programs` endpoint on `Groot.Api` (or a static JSON directory
behind Caddy) serving `id` + `version`; the app pulls new and updated definitions without an app
update. Same JSON schema, so the engine does not care where a program came from.
