using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// GZCLP for real: 26 weeks, three sessions a week, through the store. Every session is planned
/// from history before it is logged, the same discipline <see cref="GzclpBlockTests"/> uses over
/// seven sessions, run long enough to hit the path that block never reaches — a lift missing
/// enough sessions in a row to exhaust the fail ladder and reset.
/// <para>
/// Squat T1 misses its top set three times running, starting at its sixth A1. GZCLP's ladder is
/// two rungs deep (5x3+ → 6x2+ → 10x1+), so three consecutive misses is the minimum that forces a
/// reset rather than just a drop: miss one drops a rung, miss two drops the last rung, miss three
/// resets to 90% of whatever was on the bar. Every other lift trains clean, so the run also proves
/// six months of ordinary progression doesn't silently stall or drift.
/// </para>
/// </summary>
public sealed class SixMonthGzclpTests
{
    private const string Device = "phone";
    private const int SessionCount = 78; // 26 weeks x 3 sessions/week
    private static readonly DateOnly Start = new(2027, 1, 4); // a Monday

    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");

    /// <summary>What goes on the bar the first time a slot is trained, before history exists.</summary>
    private static readonly Dictionary<ExerciseSlot, decimal> OpeningWeights = new()
    {
        [new("squat", 1)] = 60m,
        [new("squat", 2)] = 40m,
        [new("bench-press", 1)] = 45m,
        [new("bench-press", 2)] = 30m,
        [new("overhead-press", 1)] = 30m,
        [new("overhead-press", 2)] = 20m,
        [new("deadlift", 1)] = 70m,
        [new("deadlift", 2)] = 50m,
        [new("dumbbell-row", 3)] = 22.5m,
        [new("dumbbell-curl", 3)] = 12.5m,
        [new("dumbbell-lateral-raise", 3)] = 7.5m,
    };

    /// <summary>Which A1 occurrences squat's top set comes up short on: three in a row.</summary>
    private static readonly HashSet<int> SquatMisses = [6, 7, 8];

    [Fact]
    public async Task Six_months_of_gzclp_runs_clean_through_a_ladder_reset()
    {
        using var store = new TemporaryDatabase();
        var userId = await store.CreateUser();
        await store.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        var date = Start;
        var a1Count = 0;
        var seenDays = new List<string>();
        decimal? squatWeightBeforeReset = null;
        var sawReset = false;

        for (var session = 1; session <= SessionCount; session++)
        {
            var dayKey = await store.Progress.NextLiftDay(userId, Gzclp);
            Assert.True(Gzclp.Rotates(dayKey), $"session {session}: '{dayKey}' fell out of the rotation");

            var history = await store.Sessions.Between(userId, Start, date);
            var state = LiftProgressionHistory.Replay(Gzclp, history);
            var plan = LiftSessionBuilder.For(Gzclp, dayKey, state);

            if (dayKey == "A1") a1Count++;
            var missSquatTopSet = dayKey == "A1" && SquatMisses.Contains(a1Count);

            // Track the weight squat is standing on right before the three misses land, so the
            // reset can be checked against the actual formula rather than a hand-guessed number.
            if (dayKey == "A1" && a1Count == SquatMisses.Min())
            {
                squatWeightBeforeReset = plan.Exercises.Single(e => e.ExerciseId == "squat").TargetKg;
            }

            // The plan for the A1 right after the third consecutive miss is where the reset
            // shows up: `state` was replayed from history BEFORE today's session is logged, so
            // it carries the reset and nothing today's own outcome would add on top of it.
            if (!sawReset && dayKey == "A1" && a1Count == SquatMisses.Max() + 1)
            {
                var squat = state[new ExerciseSlot("squat", 1)];
                Assert.Equal(0, squat.Stage); // the ladder restarted at 5x3+
                Assert.Equal(Math.Round(squatWeightBeforeReset!.Value * 0.9m, 1), squat.WorkingWeightKg);
                sawReset = true;
            }

            // Invariant, every session: nothing the screen would show is negative or missing for
            // a lift that has been trained before.
            foreach (var exercise in plan.Exercises)
            {
                if (state.ContainsKey(new ExerciseSlot(exercise.ExerciseId, exercise.Tier)))
                    Assert.True(exercise.TargetKg is >= 0m, $"session {session} {exercise.ExerciseId}: negative or missing target");
            }

            var sessionId = Guid.NewGuid();
            var order = 0;
            var sets = new List<SetEntry>();

            foreach (var exercise in plan.Exercises)
            {
                var slot = new ExerciseSlot(exercise.ExerciseId, exercise.Tier);
                var weight = exercise.TargetKg ?? OpeningWeights[slot];
                var missThisLift = missSquatTopSet && exercise.ExerciseId == "squat";
                var lastIndex = exercise.Sets.Count - 1;

                sets.AddRange(exercise.Sets.Select(set => SetEntry.Total(
                    sessionId, exercise.ExerciseId, order++, weight,
                    reps: missThisLift && set.Index == lastIndex ? set.TargetReps - 1 : set.TargetReps)));
            }

            await store.Sessions.Save(
                LoggedSession.Lift(sessionId, userId, date, Gzclp.Id, dayKey, sets),
                updatedAt: date.DayNumber,
                Device);

            seenDays.Add(dayKey);
            date = date.AddDays(session % 7 == 5 ? 3 : 2); // Mon/Wed/Fri-ish, three a week
        }

        Assert.True(sawReset, "the scripted misses never produced a reset — check SquatMisses against the fail ladder depth");

        // Every day of the rotation actually got trained, repeatedly, over six months.
        foreach (var day in Gzclp.Rotation)
            Assert.True(seenDays.Count(d => d == day) >= 15, $"'{day}' trained fewer times than a 26-week block should allow");

        var final = LiftProgressionHistory.Replay(Gzclp, await store.Sessions.Between(userId, Start, date));

        // Every lift that trained clean the whole way climbed; none of it depends on squat's detour.
        Assert.True(final[new ExerciseSlot("bench-press", 1)].WorkingWeightKg > OpeningWeights[new("bench-press", 1)]);
        Assert.True(final[new ExerciseSlot("deadlift", 1)].WorkingWeightKg > OpeningWeights[new("deadlift", 1)]);
        Assert.True(final[new ExerciseSlot("overhead-press", 1)].WorkingWeightKg > OpeningWeights[new("overhead-press", 1)]);

        // Squat T1 is past its reset and climbing again, not stuck at the reset floor.
        var squatFinal = final[new ExerciseSlot("squat", 1)];
        Assert.Equal(0, squatFinal.Stage);
        Assert.True(squatFinal.WorkingWeightKg > Math.Round(squatWeightBeforeReset!.Value * 0.9m, 1));

        // The rack can still build every weight the six months landed on — the plate solver
        // doesn't quietly fall over on numbers a real block actually produces.
        foreach (var (slot, exerciseState) in final)
        {
            if (Gzclp.TierFor(slot.Tier) is { } tier
                && Gzclp.Days.SelectMany(d => d.Exercises).FirstOrDefault(e => e.ExerciseId == slot.ExerciseId)?.Loading == LoadingKind.Barbell)
            {
                Assert.NotNull(EquipmentProfile.Rack.Round(exerciseState.WorkingWeightKg));
            }
        }
    }
}
