# Groot — Habit System & Growth Visualization (worked out)

*Extends research.md §7. Status: design proposal 2026-08-18. Mockup: `design/habit-rings.html`.*

---

## 1. Core idea: Template ≠ Contract

Two separate things, deliberately decoupled:

- **Template** — how you *plan* the week. Paints days:
  `Mon C25K · Tue WL · Wed C25K · Thu WL · Fri C25K · Sat WL · Sun REST`
- **Contract** — what *keeps the streak*. Counts, not days:
  **≥2 × WL, ≥2 × RUN, ≥1 × REST per ISO week, jokers may fill activity gaps.**

Doing the template = 3+3+1, comfortably above contract. Life happens = contract still reachable.
Moving Tuesday's lift to Wednesday breaks nothing — the template is a suggestion layer (drives the
morning notification "Today: C25K W3·D2 — 28 min"), the contract is the rule layer. This is what
makes it a habit tracker instead of a schedule that punishes you for living.

### 1.1 Contract rules (precise)

1. Week = 7-day block anchored on the user's **week-start setting** — not hardcoded ISO. Default
   comes from locale (`CultureInfo.DateTimeFormat.FirstDayOfWeek`: Monday for nl/most of EU, Sunday
   for en-US), overridable in settings. Evaluation at the last day's 23:59 local time ("week close").
   All contract math, the grid rows, and the `weeks` table key derive from this one setting; changing
   it mid-streak re-anchors from the next week (historic closed weeks stay as evaluated).
2. A session credits its type **once per calendar day** (two lifts on Monday = 1 WL credit — no
   Sunday-night gaming of the week).
3. **REST** = at least one calendar day with zero logged sessions. Auto-detected — no button needed
   (but an optional "rest kept ✓" tap exists for the psychology of *claiming* it).
4. **Jokers: 2 per week, reset Monday, no rollover.** A joker substitutes one missing *activity*
   credit (WL or RUN). Auto-spent at ring close, oldest gap first; can also be pre-played
   ("Thursday is gone, spend it now") for peace of mind.
5. Jokers never buy REST. Trained 7/7? Ring is kept (don't punish enthusiasm) but gets an
   **overgrowth mark** and a nudge — rest is part of the contract philosophy, the app says so.
6. **Streak = consecutive weeks with the ring closed.** Day-streaks are hostile to lifters;
   week-streaks match the actual training rhythm. Secondary display: "this week 4/5 slots".
7. Broken week: streak resets, history keeps a visible scar (see viz) — honesty over shame-hiding.
8. Phase 2: freeze mode (sick/vacation) pauses the tree's clock entirely — distinct from jokers.

### 1.2 Why 2 jokers is right

1 joker = one bad day kills motivation for the rest of the week ("week's dead anyway").
3+ = contract stops meaning anything (2 WL + 2 RUN with 2 jokers already allows a 2-session week).
2 matches the user's own instinct and Duolingo-scale forgiveness research. Tune later per-user
(setting: 0–3, default 2).

---

## 2. Weight entry: per-side first-class

Three entry modes, per-exercise sticky default:

| Mode | Input | Canonical stored total | Used for |
|---|---|---|---|
| **Per-side** | bar preset + plates/side | `bar + 2 × side` | barbell lifts (default for Squat/Bench/DL/OHP/Row) |
| **Total** | one number | as typed | machines, cables, fixed bars ("just set 90 kg") |
| **Per-hand** | dumbbell weight | `2 × hand` (volume math) | DB work |

User's example renders as: **`[bar 20] + 25 /side → 70.0 kg`** — the total is always shown live
next to the input, and the per-side entry *is* the plate calculator (no separate screen in this mode).
Total mode keeps the classic plate-breakdown hint underneath (`90 → /side: 25+10`).

Bar presets: 20 (men's), 15 (women's), 10 (technique), custom (SSB 25, trap bar 27.5…), sticky per
exercise. **Owner's actual bar: ATX Professional Bar 30 mm = 11 kg** — proof the default must be an
*equipment profile*, not an assumption (first live parse assumed 20 and was 9 kg wrong).

**Equipment profile** (settings): list of bars (name + kg) and the **plate inventory** (30 mm home
plates here: counts per 1.25/2.5/5/10/15/20/25 kg). Two consumers:
1. the plate calculator (obviously), and
2. the **progression engine — targets round to the nearest *achievable* load**: +2.5 on 48.5 kg is
   only offerable if the inventory can build 51.0/side-symmetric on an 11 kg bar. No more "app says
   52.5, rack says impossible".

### 2.1 Mixed units (kg barbell + lb PowerBlock in the same session)

Owner's rack: ATX bar + 30 mm plates in **kg**, PowerBlock adjustable dumbbells stepped in **lb**.
Consequences:

- **Unit is a property of the equipment, not the app.** Global unit setting only provides the
  *default* and the stats display unit. Each equipment-profile item carries its own unit; exercises
  inherit from their sticky equipment (Squat → ATX/kg, DB Row → PowerBlock/lb). Both can appear in
  one workout — labels always say which ("2× 35 lb · 15.9 kg each").
- **PowerBlock = stepped inventory, same law as plates.** Not continuous: a configurable list of
  selectable per-hand weights (model-dependent — 2.5/5 lb steps, stage kits differ; user configures
  their model once). Per-hand entry mode snaps to steps; **progression proposes the next achievable
  step**, exactly like plate-rounding on the bar. One abstraction covers both:
  `Equipment { unit, achievable_loads[] }` — bar+plates *compute* the list, PowerBlock *declares* it.
- **Storage stays canonical kg, intent stays lossless**: `weight_kg` (canonical, full precision —
  35 lb → 15.87573 kg, never round-tripped) + `entry_weight`/`entry_unit` (what the user actually
  picked, for faithful display/edit). Analytics, e1RM, tonnage, export: kg. Display: native.
  Rounding happens at render time only — conversions never accumulate.

### 2.2 Switching equipment mid-anything + adding equipment easily

Requirement sharpened: kg dumbbells *or* lb PowerBlock **per set, depending on the work** — sticky
defaults are not enough, switching must be one tap at the point of entry.

- **Equipment chip row lives inside the weight entry widget**: `[ATX bar · kg] [PowerBlock · lb]
  [Fixed DB · kg] [+]`. One tap = entry unit, increments, and achievable-load snapping all swap.
  Last-used-per-exercise stays the *default*; the chips make the exception cost one tap, zero
  settings-diving. Alternating equipment between sets of the same exercise: fine.
- **Sets store `equipment_id`** — history renders faithfully ("3×12 @ 35 lb · PowerBlock" vs
  "3×12 @ 16 kg · DB"), analytics stay canonical-kg and don't care.
- **Targets speak per-side, data speaks total.** Progression prompts render in rack language —
  *"add 1.25/side → 63.5 kg"* — because that's how loading actually happens.
- **Nominal bar weight (`counts_as`) — owner's design, initially misjudged by the reviewer.**
  The original proposal — *"'pretend' the 11 kg is 10 for plate/progress calculations"* — always
  meant a nominal layer over a preserved real value (the quotes around 'pretend' said so). First
  review misread it as falsifying the stored weight and rejected it; challenged and overturned:
  (1) the app already ignores collar mass (~0.5 kg) and 30 mm plate tolerance (±2–3%) — the 1 kg
  "truth" was false precision; (2) a constant offset is shift-invariant — LP progression, PRs,
  e1RM trends, tonnage deltas all unaffected; (3) **milestones**: on an 11 kg bar, totals end in
  …1/…6 (or ….5) — an exact 100 kg squat is unreachable; nominal-10 restores round-number
  psychology, which is motivation infrastructure in a habit app. Remaining risk (frame-mixing with
  true-weight 20 kg gym bars) is contained because the pretend is per-equipment, not global.
  **Implementation (the owner's proposal, formalized)**: bars get `actual_kg` (11) + optional
  `counts_as` (10); totals/targets/milestones/display use `counts_as ?? actual_kg`; `actual_kg`
  preserved in the equipment record. Home-bar templates may suggest a `counts_as` default.
- **Adding equipment = template picker, not a form.** "+" chip → library:
  - Bars: Olympic 20 · women's 15 · technique 10 · *home 30 mm (10/11/12 — pick)* · custom kg
  - Adjustable DBs: PowerBlock models w/ stage kits (declared lb steps), spinlock custom
  - Fixed DBs/KBs: range wizard — "4–40 kg in 2 kg steps" → generates `achievable_loads`
  - Plates: per-denomination count stepper (1.25×2, 2.5×4, …) → computed loads
  - Everything archivable, never deletable (history holds references).
- Onboarding asks equipment before first workout (3 taps for the common cases). Phase-3 sugar:
  point the camera at the rack and let the AI draft the profile (`IAdvisor` use case #5).

Data model delta (`sets` table): `entry_mode`, `bar_kg?`, `side_kg?`, `entry_weight`, `entry_unit`,
`equipment_id` added; `weight_kg` stays the canonical total — analytics/e1RM/export never care how
it was typed. CSV import maps to `total` (the Strong format's dual KG/LB columns confirm the pattern).
`equipment(id, name, kind: bar|adjustable_db|fixed_db|kb|other, unit, params_json, archived)`.

---

## 3. Running: C25K as a first-class program type

Program engine grows a second species. Same `programs.json_definition`, new `type`:

- **`sets_reps`** — existing: exercises, sets, progression rules (GZCLP, GreySkull…).
- **`intervals`** — ordered segments: `[{walk, 300s}, {run, 90s}, {walk, 90s}, {run, 180s}, …]`.
  C25K = 9 weeks × 3 sessions, shipped as built-in program (the plan itself is generic public
  knowledge — implement from scratch, don't copy any app's descriptions/audio. Note: "Couch to 5K /
  C25K" is trademarked in some jurisdictions (NHS/active.com usage) — ship as **"0→5K"** with
  "couch-to-5k style" only in descriptive text. Same lane as the GreySkull naming caution.)

### 3.1 Interval runtime (the beep/voice requirement)

Reuses the exact infrastructure the rest timer already demands — one foreground service, two clients:

- **Segment state machine** in `Groot.Core` (pure C#, unit-testable): current segment, elapsed,
  next-up, total remaining.
- **Android**: foreground service (`mediaPlayback` type) + chronometer notification per segment
  ("RUN · 1:23 left · next: walk 3:00" + [pause] [skip] actions). Segment change → `ToneGenerator`
  beep pattern (run = 2 high beeps, walk = 1 low, last 10 s = tick) + optional **TTS** voice
  ("Run — ninety seconds") via `TextToSpeech` with **audio focus ducking** (music dips, cue speaks,
  music returns — non-negotiable for runners).
- **iOS**: background audio session + `AVSpeechSynthesizer`; local notification fallback per segment.
- Screen-off is the design target: pocket phone, headphones, zero interaction for 28 minutes.
- GPS — **in MVP, route stored (owner decision 2026-09-01, reversing 2026-08-18).** The MVP is the
  app the owner takes outside, so a run records where it went: MAUI Geolocation feeds `RouteTrack`,
  the run screen draws it, and `SessionMetricsStore` keeps it. The 2026-08-18 position was GPS-lite
  at MVP+1, coordinates discarded immediately, on the grounds that a route track identifies the
  runner's front door and conflicts with the no-PII architecture. That reasoning stands and is
  answered by scope, not denial: **the route stays on the device.** Routes sync only if that is
  decided explicitly, opt-in, never as a silent default — `Groot.Api` has not reached the question.
  Still true and still unbuilt: raw unfiltered GPS pace is jittery enough to demotivate, so a
  distance or pace shown as a number wants outlier rejection and smoothing first. 0→5K stays
  time-based; the route is what the run drew, not what drives the intervals.
- Health sync **in MVP** (Android, owner decision 2026-08-18): write `ExerciseSessionRecord(running)`,
  read sleep for recovery context. iOS `HKWorkout` when the iOS head lands.

### 3.2 Voice cues (TTS) — "Almost done with the warm-up, get ready for your first run"

**Cue model**: each segment carries optional cue points — `{ at, key, args }` where `at` is seconds
from segment start (≥0) or from segment end (negative). The interval state machine (pure C#) emits
`CueDue(key, args)` events; platforms only speak.

**Text**: cue keys resolve through the same i18n resource pipeline as the UI
(`cue.warmupEnding` → "Almost done with the warm-up — get ready for your {ordinal} run"), args
computed from program context (which run is next, remaining time). Dutch locale speaks Dutch — free.

**Android** (`Platforms/Android/TtsCueService.cs`) — platform `TextToSpeech`, offline-capable, zero cost:

```csharp
public sealed class TtsCueService : Java.Lang.Object, TextToSpeech.IOnInitListener
{
    TextToSpeech? _tts; AudioManager? _audio; AudioFocusRequestClass? _focus;

    public void Init(Context ctx)
    {
        _tts = new TextToSpeech(ctx, this);
        _audio = (AudioManager)ctx.GetSystemService(Context.AudioService)!;
    }
    public void OnInit(OperationResult status) => _tts!.SetLanguage(Java.Util.Locale.Default);

    public void Speak(string text)
    {
        _focus = new AudioFocusRequestClass.Builder(AudioFocus.GainTransientMayDuck)
            .SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.AssistanceNavigationGuidance)
                .SetContentType(AudioContentType.Speech).Build()!)
            .Build();
        _audio!.RequestAudioFocus(_focus);          // music volume dips
        _tts!.Speak(text, QueueMode.Add, null, "cue");
        // UtteranceProgressListener.OnDone → _audio.AbandonAudioFocusRequest(_focus)
    }
}
```

Key points: `GainTransientMayDuck` = Spotify dips instead of pausing; TTS runs inside the existing
foreground service so cues fire screen-off; beep (ToneGenerator) precedes voice by ~300 ms so the
duck is already active when speech starts.

**iOS** — `AVSpeechSynthesizer` with a ducking audio session:

```csharp
var session = AVAudioSession.SharedInstance();
session.SetCategory(AVAudioSessionCategory.Playback,
    AVAudioSessionCategoryOptions.DuckOthers | AVAudioSessionCategoryOptions.MixWithOthers);
session.SetActive(true);
_synth.SpeakUtterance(new AVSpeechUtterance(text)
    { Voice = AVSpeechSynthesisVoice.FromLanguage(currentLocale) });
```

Background audio entitlement (`UIBackgroundModes: audio`) keeps cues alive with the screen off.

**Settings**: voice cues on/off · beeps only · voice+beeps (default) · cue verbosity (all / segment
changes only). No cloud TTS anywhere — platform voices are offline, free, and locale-matched.

---

## 4. The visualization: rings × grid ("GitHub commit history like thing")

User instinct is right — the contribution grid is the correct *utility* view for habits. But the
grid alone is generic (every habit app has one). Resolution: **grid for weeks, rings for life** —
two zoom levels of the same organism, both in Groot's botanical language.

### Concept A — "This week" contract card (the daily driver)
Seven day-slots Mon–Sun, template-tinted. Filled by logging: moss leaf = WL, amber leaf = RUN,
bark dot = REST, droplet = joker spent. Contract meter underneath: `WL ●● · RUN ●◐ · REST ○ · 💧💧`.
This is the habit-tracker structure — glance = "what does the week still need from me".

### Concept B — Season grid (the GitHub view, skinned botanical)
7 rows (Mon–Sun) × last 26 weeks. Cells are **leaves, not squares**: moss intensity = lift tonnage,
amber = run minutes, split-leaf = double day, bark = rest, droplet outline = joker day, withered
gray = broken-week days. Closed weeks get a thin vine underline connecting their column — streaks
literally grow a stem along the bottom. **Animation**: on log, today's leaf sprouts (scale+unfurl,
250 ms); on ring close, the week's vine segment draws left→right; on streak break, the vine snaps
(one-time, subtle). Density, familiarity, motivation — the commit-graph dopamine, de-generic'd.

### Concept C — Growth rings (the lifetime view, evolved from v2)
**One ring per closed week** (not per session — rings must mean something). Tree cross-section:
- ring thickness = week volume (tonnage + run minutes, normalized)
- ring hue = activity mix (moss ↔ amber gradient)
- joker week = small **knot** in the ring (visible, not ugly — trees keep their knots)
- broken week = hairline **scar** gap, then rings continue (history stays honest)
- year boundary = darker band; PR weeks = tiny branch bud on the rim
- Display: rolling 52 rings + "tree age: 87 weeks"; tap ring → week summary; pinch = years.
Ceremony: Sunday ring close animates the new ring growing outward (1 s radial sweep) + week recap
card (tonnage, km, PRs, jokers spent) — the shareable moment, phase 2.

### Concept D — considered, parked
Branch timeline (weeks as growing branch segments, PRs as buds): most original, weakest at-a-glance
readability, hardest to render well. Archive in Research/UI if ever revisited.

**Recommendation: A + B on the home screen (MVP), C as the "Growth" tab (MVP-lite: static rings,
animate later).** A answers *today*, B answers *lately*, C answers *who have I become* — three
time horizons, one metaphor, no character anywhere.

**UPDATE (2026-08-21): metaphor swap, owner decision.** Concept C is now a barbell loaded with
plates, viewed head-on — not a tree cross-section. Concentric rings read as plates stacked on the
sleeve. Fits the app domain directly (no tree needed at all, finishing what §6b started) and reuses
the same ring-per-closed-week structure:
- ring thickness = plate diameter for that week (from week volume, normalized — thicker plate,
  bigger week)
- ring color = standard plate color coding (25 kg red, 20 kg blue, 15 kg yellow, 10 kg green,
  5 kg white; run-heavy weeks lean a run accent instead of a lift-plate color)
- joker week = a collar/clip mark on the rim
- broken week = a gap in the plate stack (bar visible through the hairline)
- year boundary = a change of bar sleeve (visual break in the stack); PR weeks = a small chalk
  mark on the rim
- center = the bar sleeve/collar, not a growth-ring pith
- Display and interaction unchanged (rolling rings, tap for week summary, pinch for years) —
  only the render metaphor changes; re-language "tree age: 87 weeks" to "87 weeks on the bar" or
  similar (final copy pass owed against §5b humanizer rules).

Tree-cross-section language above is superseded by this update; kept for history.

---

## 5. Screens touched (mockup shows all)

1. **Home / This week**: contract card (A) + streak + mini season grid (B).
2. **Session runner** (existing v2 screen, unchanged) + per-side weight widget.
3. **Run screen**: big segment clock, RUN/WALK state color-flood, next-up line, W3·D2 progress dots.
4. **Growth tab**: rings (C) + season grid full.
5. **Notification shade**: run-interval variant next to the rest-timer variant.

## 5b. Copy voice (owner call, 2026-08-18: mockups failed the /humanizer check)

All user-facing strings follow the humanizer rules. The tells that actually slipped into our own
mockups, so they're the house blacklist:

- negative parallelisms ("Slow is fine; stopping is not")
- aphorism formulas ("grid for weeks, rings for life")
- em dashes in UI strings and spoken cues (TTS pauses are commas or periods)
- triple negation lists ("no tree character, no face, no limbs")

House rules instead: say the concrete thing ("the grid shows recent weeks"), numbers over adjectives,
cues are short spoken sentences a coach would actually say. Resource-key review = run the humanizer
checklist over `*.resx` before release.

## 6. Data model delta

```
weeks(id, user_id, week_start_date, contract_met, jokers_spent, overgrowth, closed_at)  -- materialized at week close; keyed by start date, not ISO week
sessions(id, user_id, date, type: wl|run|rest_claim, program_id?, duration_s?, ...)  -- run sessions join segments
sets: + entry_mode, bar_kg?, side_kg?          -- weight entry modes (§2)
programs: + type: sets_reps | intervals
settings: jokers_per_week (default 2), week_start (default: locale FirstDayOfWeek — Mon EU, Sun US; user-overridable)
```

## 6b. Terminology update (2026-08-18): plant references removed

Owner decision: keep the design language (palette, Fraunces/Public Sans, layout, warmth), drop the
botanical metaphors. "groot" is simply the Dutch word for big; no tree is needed to carry it.

| Was | Now |
|---|---|
| leaves (grid cells) | plain rounded cells (github-style) |
| vine underline | streak bar |
| tree age | "week N overall" |
| ring close ceremony | week close + recap |
| knot (joker week) | joker mark |
| scar (broken week) | gap |
| 🌿/🍂/💧 icons | geometric: lift ▲ · run ● · rest ☾ · joker ◆ |
| "Growth" tab | "Progress" tab |
| growth rings | **year rings** (kept — abstract concentric data-viz, Apple-activity-rings precedent, no plant needed) |

Palette color *names* (moss/bark/amber) are internal CSS tokens — invisible to users, keep.

**UPDATE (same day): decision reopened.** Owner likes the plants ("strong like a tree is cool") —
the actual conflict is the *combination* of plants and the name, which reads as a character rather
than as a system. Both variants now exist for comparison:
- `design/habit-rings.html` — neutral (geometric icons, year rings)
- `Research/UI/habit-rings-plants.html` (archived there when it was rejected) — plant identity

The fork in the road (pick one):
1. **Keep "Groot" → neutral variant.** Zero character adjacency, palette/rings keep most of the warmth.
2. **Keep plants → rename the app.** Cleanest overall: identity the owner actually loves, trademark
   worry dies permanently. Dutch candidates with tree-strength meaning:
   - **Stam** — trunk *and* tribe/lineage; 4 letters; doubles nicely if social features ever come
   - **Eik** — oak; strength *symbol* only — CORRECTION (owner caught it): "zo sterk als een eik"
     is NOT an established Dutch idiom (databases list paard/os/leeuw/Simson; the oak version is an
     anglicism). Weakens Eik's case vs Stam.
   - **Kernhout** (EN: *Heartwood*) — the dense, strongest wood at a tree's core; great metaphor
   - **Jaarring** — year ring; names the signature viz itself
3. Keep both plants *and* Groot — livable for a personal app never marketed, but the risk is
   permanent management instead of a one-time fix.

Recommendation was option 2 ("Stam"); **owner DECIDED otherwise, 2026-08-18: option 1 — the name
"Groot" stays, plant/tree language goes.** Neutral variant (`design/habit-rings.html`) is canonical;
plant variant archived at `Research/UI/habit-rings-plants.html`. Palette (moss/bark/amber tokens),
Fraunces, rings-as-abstract-data-viz all stay. No tree words, no leaves, no mascots — which also
settles the identity question permanently: name without imagery is the lane.

## 7. Open questions

1. Overgrowth (7/7, no rest): warn-only (recommended) or hard-break the ring? — mockup assumes warn-only.
2. Joker auto-spend at ring close vs. manual-only? — recommended: auto with pre-play option.
3. Do run *and* lift on the same day both credit? (recommended: yes, still once per type per day).
4. Grid horizon on phone: 26 weeks visible (recommended) vs full 52 with horizontal scroll.
5. "0→5K" naming vs licensing an actual C25K brand — descriptive-only for now.
