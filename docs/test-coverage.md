# What the tests cover

276 tests across three projects. This file is the map: what is proven, what a failure would
mean, and what is not covered yet. Regenerate the counts with

```
dotnet test tests/Groot.Core.Tests    # 163
dotnet test tests/Groot.Data.Tests    #  45
dotnet test tests/Groot.UI.Tests      #  68
```

## The seven-session block, weight by weight

`tests/Groot.Data.Tests/GzclpBlockTests.cs` walks GZCLP through seven sessions in the store.
Every session is checked before it is logged, so a wrong number names the session it first
appeared in. This table is what the test asserts.

Each cell is the weight in kg that goes on the bar that session. A blank cell is a slot the day
does not train. GZCLP's rotation is four days long and its week is three sessions, so the two
drift apart on purpose.

| Slot | 1 · A1 | 2 · B1 | 3 · A2 | 4 · B2 | 5 · A1 | 6 · B1 | 7 · A2 | next time |
|---|---|---|---|---|---|---|---|---|
| squat T1 `5x3+` | 60 | | | | 62.5 | | | 65 |
| bench press T2 `3x10` | 30 | | | | 32.5 | | | 35 |
| chin-up T3 `3x15+` | 0 | | | | 0 | | | 0 |
| overhead press T1 `5x3+` | | 30 | | | | 32.5 ✗ | | 32.5 `6x2+` |
| deadlift T2 `3x10` | | 50 | | | | 52.5 | | 55 |
| dumbbell row T3 `3x15+` | | 22.5 | | | | 22.5 | | 22.5 |
| bench press T1 `5x3+` | | | 45 | | | | 47.5 | 50 |
| squat T2 `3x10` | | | 40 | | | | 42.5 | 45 |
| dumbbell curl T3 `3x15+` | | | 12.5 | | | | 12.5 | 12.5 |
| deadlift T1 `5x3+` | | | | 70 | | | | 75 |
| overhead press T2 `3x10` | | | | 20 | | | | 22.5 |
| dumbbell lateral raise T3 `3x15+` | | | | 7.5 | | | | 7.5 |

`✗` is the miss: the press comes up one rep short on its last set in session 6.

What the table is there to make visible:

- **Squat and bench appear twice, on separate ladders.** Squat T1 climbs 60, 62.5, 65 while squat
  T2 sits at 40 and then 42.5. One is 5x3+, the other 3x10; sharing a number would put the heavy
  weight on a set of ten.
- **A miss holds the weight and drops a rung.** The press stays at 32.5 and comes back as 6x2+,
  not as 5x3+ again.
- **The deadlift adds 5 kg where every other lift adds 2.5.** Its T1 increment is an override in
  the program JSON.
- **T3 does not move.** Three sets of fifteen is 45 reps in the session but 15 on the set that
  counts, and the threshold is 25 on that set. A T3 climbs when the AMRAP set earns it, not when
  the session total happens to be large.
- **Session 7 is unaffected by session 6.** None of A2's lifts were in the missed session, so all
  three carry the increment they earned in session 3.

## Coverage by area

`bug` marks a test that encodes a defect this work found and fixed.

### Schema and connection · `SchemaTests`

| Test | What a failure would mean |
|---|---|
| `Opening_an_empty_file_applies_the_schema_and_records_its_version` | The schema is not applied, or `user_version` is not stamped |
| `Reopening_an_existing_database_keeps_its_data_and_its_version` | A restart loses data, or re-runs the schema |
| `Foreign_keys_are_enforced_so_an_orphan_session_cannot_be_written` | `ForeignKeys=true` is not reaching the connection |
| `A_lifting_session_without_a_rotation_day_is_refused_by_the_schema` | A day-less lift stores, renders, credits the week, and then falls out of progression without a sound |
| `A_run_without_a_place_in_its_program_is_refused_by_the_schema` | The same hole on the interval side |

### Session round-trip · `SessionStoreTests`

| Test | What a failure would mean |
|---|---|
| `A_lift_session_and_its_sets_come_back_exactly_as_they_were_saved` | A column is mistyped, or an enum or decimal does not survive storage. Covers all three entry modes, including a per-hand dumbbell in pounds |
| `A_run_session_comes_back_exactly_as_it_was_saved` | Interval week and day do not round-trip |
| `A_session_survives_closing_and_reopening_the_database` | The store does not actually persist, which is the whole point of it |
| `Saving_a_session_again_replaces_its_set_list_rather_than_adding_to_it` | Deleting a set leaves it in the database |

### Sync semantics · `SessionStoreTests`

| Test | What a failure would mean | |
|---|---|---|
| `A_local_save_wins_even_when_it_lands_in_the_same_millisecond_as_the_last_one` | The user's second tap is silently dropped | bug |
| `A_session_arriving_older_than_the_stored_one_is_ignored_along_with_its_sets` | A stale row from the server overwrites newer local work |  |
| `Two_sessions_arriving_with_the_same_timestamp_resolve_the_same_way_every_time` | Two devices each keep their own version and never converge |  |
| `A_deleted_session_stops_being_readable_but_keeps_its_row_for_sync` | A deletion either shows up in reads or cannot travel |  |

### Ownership · `SessionStoreTests`

There is one account today. These tests exist because the signatures they exercise are what a
request handler will call once `Groot.Api` is written, and a handler holding a route id cannot
scope a query the method never asked it to scope.

| Test | What a failure would mean |
|---|---|
| `One_account_cannot_read_or_delete_another_accounts_session` | `Find` and `Delete` are reachable across accounts |
| `A_session_arriving_under_another_accounts_ownership_is_refused` | A sync payload can rewrite someone else's session in place and have them read it back as their own |
| `Two_accounts_that_own_the_same_bar_keep_their_own_copy_of_it` | An equipment id is a slug, so two racks called "atx" would be one row overwriting each other |

### History queries · `SessionStoreTests`

| Test | What a failure would mean | |
|---|---|---|
| `The_week_reduces_to_the_dates_and_kinds_the_contract_counts` | The week window is off by a day at one end |  |
| `Daily_counts_group_by_day_and_skip_days_with_nothing_on_them` | The history grid counts wrong |  |
| `Daily_counts_over_an_empty_history_come_back_empty_rather_than_throwing` | A first run throws, because an aggregate over no rows has no type to infer | bug |

### Equipment and settings · `EquipmentAndSettingsStoreTests`

| Test | What a failure would mean |
|---|---|
| `The_rack_comes_back_with_its_bar_and_every_plate_pair` | Plate maths reads a profile that is not the one that was saved |
| `A_bar_keeps_the_difference_between_what_it_weighs_and_what_it_counts_as` | The ATX's 11-versus-10 collapses and every logged total is a kilo off |
| `A_plate_written_as_five_point_zero_is_the_same_plate_as_five` | The plate key splits on formatting and the solver thinks the rack has four fives |
| `Fixed_loads_survive_the_round_trip_and_stay_in_the_equipments_own_unit` | A pound-denominated dumbbell comes back in the wrong unit or loses its load list |
| `Saving_a_profile_again_replaces_its_plates_rather_than_adding_to_them` | Removing a plate leaves it in the database |
| `A_rack_can_lose_its_last_plate` | The empty-list transition is the one that could silently keep the old plate set |
| `Unknown_equipment_reads_as_null_rather_than_as_an_empty_profile` | A missing bar reads as a bar with no plates |
| `A_local_equipment_save_wins_in_the_same_millisecond_as_the_last_one` | A correction made right after a save is silently dropped |
| `An_older_equipment_profile_arriving_from_another_device_is_ignored` | A stale profile overwrites a newer one, plates included |
| `Settings_round_trip_the_week_start_the_contract_maths_keys_off` | The week starts on the wrong day and every contract evaluation shifts |
| `An_account_with_no_saved_settings_reads_as_null_rather_than_as_defaults` | The store invents Monday and hides that nobody has chosen |
| `A_local_settings_save_wins_in_the_same_millisecond_as_the_last_one` | Same dropped-second-write bug, in settings |
| `Older_settings_arriving_from_another_device_are_ignored` | A stale week start overwrites a newer one |
| `Settings_survive_closing_and_reopening_the_database` | Settings do not persist |

### Program sequencing · `ProgramSequenceTests`, pure Core

| Test | What a failure would mean |
|---|---|
| `The_rotation_advances_and_wraps` | A1, B1, A2, B2 and back to A1, as a theory over all four |
| `A_program_starts_at_the_first_day_of_its_rotation` | An empty history opens on the wrong day |
| `A_day_the_program_does_not_have_is_rejected_rather_than_guessed` | A bad day key resolves to something plausible instead of failing |
| `The_next_run_is_the_next_session_of_the_same_week` | The interval program repeats or skips a session |
| `The_last_session_of_a_week_rolls_into_the_first_of_the_next` | Week six never arrives |
| `The_final_session_of_the_program_has_nothing_after_it` | A finished program wraps back to the start |
| `An_interval_program_starts_at_week_one_session_one` | A new runner opens somewhere else |
| `A_session_the_week_does_not_have_is_rejected_rather_than_guessed` | A bad session number resolves silently |

### Progression replay · `LiftProgressionHistoryTests`, pure Core

Nothing stores a working weight. These are the spec for recomputing it.

| Test | What a failure would mean | |
|---|---|---|
| `No_history_means_no_weights_rather_than_a_guess` | The app invents a starting weight |  |
| `The_first_session_of_a_slot_sets_where_its_ladder_starts` | Seeding needs a special case, or a stored input |  |
| `The_same_lift_at_two_tiers_keeps_two_ladders` | T1 and T2 squat share a weight | bug |
| `Sessions_compound_across_weeks` | Progression does not accumulate |  |
| `A_missed_session_drops_a_rung_instead_of_adding_weight` | A miss is treated as a success |  |
| `What_went_on_the_bar_beats_what_was_planned` | Overriding the weight is forgotten next session |  |
| `Sessions_are_replayed_oldest_first_whatever_order_they_arrive_in` | Sync delivering out of order changes the answer |  |
| `Another_programs_sessions_do_not_move_this_programs_ladders` | Running two programs corrupts both |  |
| `Warmups_do_not_count_as_working_sets` | A warmup single becomes the working weight |  |
| `A_T3_that_stops_at_its_target_reps_does_not_earn_the_increment` | T3 climbs every session forever | bug |
| `A_T3_that_reaches_25_on_its_last_set_climbs` | T3 never climbs |  |
| `A_T3_abandoned_after_one_set_does_not_progress_on_that_set` | The set a lifter stopped at is read as the AMRAP, so an abandoned session adds weight | bug |

### Progression rules · `ProgressionEngineTests`, `LiftProgramTests`, `ProgramCatalogFailureTests`

| Test | What a failure would mean | |
|---|---|---|
| `T3_progresses_when_the_amrap_set_reaches_25_reps` | The threshold is off by one at the boundary |  |
| `A_high_session_total_does_not_stand_in_for_the_amrap_set` | The rule reads the session total again | bug |
| `A_scheme_with_no_amrap_set_cannot_clear_an_amrap_threshold` | A straight 3x10 progresses on an AMRAP rule |  |
| `a_t3_climbs_only_once_its_last_set_reaches_the_threshold` | The same boundary, through the planner and the real program |  |
| `the retired total-reps threshold`, in the failure table | A program file with the old key silently loses its T3 rule |  |

### The seven-session block · `GzclpBlockTests`

| Test | What a failure would mean | |
|---|---|---|
| `The_seventh_session_is_planned_from_six_sessions_of_history` | Any cell in the table above is wrong, named by the session it first went wrong in |  |
| `The_missed_lift_holds_its_weight_and_drops_a_rung_for_when_B1_returns` | The fail ladder never reaches the screen | bug |

### Rendering from the store · `StoredWeekRenderTests`, bUnit

The contract card and the history grid, rendered from data that went through SQLite and came back.

| Test | What a failure would mean |
|---|---|
| `An_empty_week_renders_an_unmet_contract_and_an_empty_history` | A first run shows the wrong counts, or throws |
| `After_a_run_and_a_lift_the_same_week_renders_one_of_each_and_two_history_cells` | Logged sessions do not reach the screen |
| `The_week_card_marks_the_days_that_were_trained` | The day slots are off by a day, or ignore the kind |

## Not covered

Named here rather than left to be discovered.

| Gap | Why it matters |
|---|---|
| Migration only ever runs 0 to 1 | The loop is real but has never stepped a populated database forward. "An older database migrates it forward" stays unproven until there is a version 2 |
| `rest_claim` sessions | They appear in the foreign-key test and nowhere else. Never counted through the contract, never rendered |
| Concurrency and WAL | Everything is single-threaded. No test opens two connections at once, so the busy timeout and the migration's in-transaction version re-read are reasoned about rather than proven |
| Program versioning | A session records which program and which day, not which *version* of that program. Editing a shipped program rewrites the history that replays through it |
| No head reads the store | `GrootLiftScene` and `GrootRunScene` still use their in-memory defaults, so none of this runs in the app yet |
| `intro` and `bodyweightVariant` in the program JSON | Declared and parsed by nothing. Config that reads as working and is not |
