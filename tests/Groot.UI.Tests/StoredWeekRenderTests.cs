using Bunit;
using Groot.Core.Contract;
using Groot.Core.Equipment;
using Groot.Core.Programs;
using Groot.Core.Sessions;
using Groot.Data.Tests;
using Groot.UI.Components;
using Groot.UI.Theme;

namespace Groot.UI.Tests;

/// <summary>
/// The two screens that read history — the week contract card and the season grid — rendered
/// from a real SQLite store, twice: once on an empty week, and again after a run and a lifting
/// day have been logged into it. Rendering the same components against the same store before and
/// after is what proves the reads are wired up, not just that the components accept parameters.
/// </summary>
public sealed class StoredWeekRenderTests : BunitContext, IAsyncLifetime
{
    private const string Device = "phone";
    private static readonly DateOnly Monday = new(2026, 8, 24);
    private static readonly LiftProgram Gzclp = ProgramCatalog.Embedded.LiftProgram("gzclp-rack");
    private static readonly IntervalProgram Couch = ProgramCatalog.Embedded.IntervalProgram("0-to-5k");

    private readonly TemporaryDatabase _store = new();

    public StoredWeekRenderTests()
    {
        Services.AddGrootUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _store.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task An_empty_week_renders_an_unmet_contract_and_an_empty_history()
    {
        var userId = await _store.CreateUser();

        var card = await RenderWeekCard(userId);
        // The counts sit inside <b> elements, so the assertion reads text, not raw markup.
        var meter = card.Find(".meter").TextContent;
        Assert.Contains("LIFT 0/2", meter);
        Assert.Contains("RUN 0/2", meter);
        // Nothing trained, so no day is active and the rest day is trivially kept.
        Assert.Contains("REST ✓", meter);
        // Four credits missing against two jokers: both are spent and the week still falls short.
        Assert.False((await Evaluate(userId)).ContractMet);
        Assert.Contains("◇◇", card.Markup);

        var history = await RenderHistory(userId);
        Assert.Contains("0 sessions", history.Markup);
        Assert.Empty(history.FindAll(".cells .cell.level-1"));
    }

    [Fact]
    public async Task After_a_run_and_a_lift_the_same_week_renders_one_of_each_and_two_history_cells()
    {
        var userId = await _store.CreateUser();
        await LogRunAndLift(userId);

        var card = await RenderWeekCard(userId);
        var meter = card.Find(".meter").TextContent;
        Assert.Contains("LIFT 1/2", meter);
        Assert.Contains("RUN 1/2", meter);
        // One lift and one run short: two jokers cover exactly that, so the week is kept.
        Assert.True((await Evaluate(userId)).ContractMet);
        Assert.Contains("◇◇", card.Markup);

        var history = await RenderHistory(userId);
        Assert.Contains("2 sessions", history.Markup);
        Assert.Equal(2, history.FindAll(".cells .cell.level-1").Count);
    }

    [Fact]
    public async Task The_week_card_marks_the_days_that_were_trained()
    {
        var userId = await _store.CreateUser();
        await LogRunAndLift(userId);

        var card = await RenderWeekCard(userId);
        var slots = card.FindAll(".days .slot");

        Assert.Equal(7, slots.Count);
        Assert.Contains("run", ClassesOf(slots[0]));
        Assert.Contains("lift", ClassesOf(slots[1]));
        Assert.Equal(["slot"], ClassesOf(slots[2]));
    }

    private static string[] ClassesOf(AngleSharp.Dom.IElement element) =>
        (element.ClassName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private async Task LogRunAndLift(Guid userId)
    {
        await _store.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 1, Device);

        await _store.Sessions.Save(
            LoggedSession.Run(Guid.NewGuid(), userId, Monday, Couch.Id, week: 1, day: 1, durationSeconds: 1800),
            updatedAt: 100,
            Device);

        var sessionId = Guid.NewGuid();
        var bar = EquipmentProfile.Rack.Bar;
        var sets = Enumerable.Range(0, 5)
            .Select(index => SetEntry.PerSide(sessionId, "squat", index, bar, sideKg: 25m, reps: 3))
            .ToArray();

        await _store.Sessions.Save(
            LoggedSession.Lift(sessionId, userId, Monday.AddDays(1), Gzclp.Id, "A1", sets),
            updatedAt: 200,
            Device);
    }

    /// <summary>What the store's week evaluates to, which is what the card is showing.</summary>
    private async Task<WeekEvaluation> Evaluate(Guid userId) =>
        ContractEvaluator.Evaluate(
            Monday,
            await _store.Sessions.ContractSessionsOfWeek(userId, Monday),
            new WeekContract());

    /// <summary>Renders the contract card the way a head does: evaluate what the store returns.</summary>
    private async Task<IRenderedComponent<WeekCard>> RenderWeekCard(Guid userId)
    {
        var contract = new WeekContract();
        var sessions = await _store.Sessions.ContractSessionsOfWeek(userId, Monday);
        var evaluation = ContractEvaluator.Evaluate(Monday, sessions, contract);
        var days = DaySlotsOfWeek(sessions);

        return Render<WeekCard>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.Evaluation, evaluation)
            .Add(x => x.Contract, contract));
    }

    private async Task<IRenderedComponent<SeasonGrid>> RenderHistory(Guid userId)
    {
        var counts = await _store.Sessions.DailyCounts(userId, Monday.AddDays(-7 * 25), Monday.AddDays(6));
        var days = counts.Select(c => new SeasonDay(c.Date, c.Sessions)).ToArray();

        return Render<SeasonGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.Today, Monday.AddDays(6)));
    }

    private static IReadOnlyList<DaySlot> DaySlotsOfWeek(IReadOnlyList<ContractSession> sessions)
    {
        string[] labels = ["MO", "TU", "WE", "TH", "FR", "SA", "SU"];

        return Enumerable.Range(0, 7).Select(offset =>
        {
            var date = Monday.AddDays(offset);
            var kinds = sessions.Where(s => s.Date == date).Select(s => s.Kind).ToArray();

            return kinds switch
            {
                [.. var all] when all.Contains(SessionKind.Lift) =>
                    new DaySlot(labels[offset], "▲", "lift", DaySlotVisual.LiftDone),
                [.. var all] when all.Contains(SessionKind.Run) =>
                    new DaySlot(labels[offset], "●", "run", DaySlotVisual.RunDone),
                _ => new DaySlot(labels[offset], "", "", DaySlotVisual.Pending),
            };
        }).ToArray();
    }
}
