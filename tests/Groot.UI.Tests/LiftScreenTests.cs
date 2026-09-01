using Bunit;
using Groot.Core.Programs;
using Groot.Core.Sessions;
using Groot.UI.Components;
using Groot.UI.Theme;

namespace Groot.UI.Tests;

/// <summary>
/// The lifting screen's session clock and its rests. Both are timing behaviour that a build
/// cannot check: the clock has to start itself when the first set is logged, and the rest after
/// the last set of a lift belongs to the lift that follows it, not the one just finished.
/// </summary>
public sealed class LiftScreenTests : BunitContext, IAsyncLifetime
{
    public LiftScreenTests()
    {
        Services.AddGrootUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private static readonly LiftProgram Program = ProgramCatalog.Embedded.LiftPrograms[0];

    private static LiftSessionPlan Plan() =>
        LiftSessionBuilder.For(Program, Program.Rotation[0], StartingWeights);

    private static readonly Dictionary<ExerciseSlot, ExerciseState> StartingWeights = new()
    {
        [new("squat", 1)] = ExerciseState.Starting(60m),
        [new("bench-press", 2)] = ExerciseState.Starting(30m),
    };

    [Fact]
    public void SessionClock_StartsOnTheFirstLoggedSet()
    {
        var cut = Render<LiftScreen>(p => p.Add(x => x.Plan, Plan()));

        Assert.Contains("not started", cut.Find(".session-clock").TextContent, StringComparison.OrdinalIgnoreCase);

        LogSet(cut, exercise: 0, set: 0);

        Assert.DoesNotContain("not started", cut.Find(".session-clock").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rest_AfterTheLastSetOfALift_CountsDownOnTheNextLift()
    {
        var cut = Render<LiftScreen>(p => p.Add(x => x.Plan, Plan()));

        var plan = Plan();
        var first = plan.Exercises[0];
        foreach (var set in first.Sets)
            LogSet(cut, exercise: 0, set.Index);

        // Logging a set is an async handler, so the rest moves on the render after the click.
        cut.WaitForAssertion(() =>
        {
            var name = cut.Find(".rest").Closest(".exercise")!.QuerySelector(".ex-name")!.TextContent;

            Assert.NotEqual(Name(first.ExerciseId), name);
            Assert.Equal(Name(plan.Exercises[1].ExerciseId), name);
        });
    }

    /// <summary>
    /// Taps a set circle. The rest clock redraws four times a second, so finding the circle and
    /// clicking it happen in one go on the renderer's thread: found separately, the tick in
    /// between takes the handler the click was going to reach.
    /// </summary>
    private static void LogSet(IRenderedComponent<LiftScreen> cut, int exercise, int set) =>
        cut.InvokeAsync(() => cut.FindAll(".sets")[exercise].Children[set].Click());

    private static string Name(string exerciseId) =>
        string.Join(' ', exerciseId.Split('-')) is { Length: > 0 } words
            ? char.ToUpperInvariant(words[0]) + words[1..]
            : exerciseId;
}
