using Bunit;
using Groot.UI.Theme;

namespace Groot.UI.Tests;

public sealed class BottomNavTests : BunitContext
{
    public BottomNavTests()
    {
        Services.AddGrootUI();
        JSInterop.Mode = JSRuntimeMode.Loose; // MudIcon needs no real JS; avoid asserting every call
    }

    [Fact]
    public void RendersAllFourDestinations()
    {
        var cut = Render<BottomNav>();

        var labels = cut.FindAll(".item .label").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Home", "Lift", "Run", "Progress"], labels);
    }

    [Theory]
    [InlineData(GrootDestination.Home)]
    [InlineData(GrootDestination.Lift)]
    [InlineData(GrootDestination.Run)]
    [InlineData(GrootDestination.Progress)]
    public void MarksTheSelectedDestinationActive(GrootDestination selected)
    {
        var cut = Render<BottomNav>(p => p.Add(x => x.Selected, selected));

        var active = cut.FindAll(".item.active");
        Assert.Single(active);
        Assert.Contains(LabelFor(selected), active[0].TextContent);
    }

    [Fact]
    public void ClickingAnItemRaisesOnSelectedChanged()
    {
        GrootDestination? raised = null;
        var cut = Render<BottomNav>(p => p
            .Add(x => x.Selected, GrootDestination.Home)
            .Add(x => x.OnSelectedChanged, d => raised = d));

        cut.FindAll(".item")[2].Click(); // Run is the third item

        Assert.Equal(GrootDestination.Run, raised);
    }

    private static string LabelFor(GrootDestination dest) => dest switch
    {
        GrootDestination.Home => "Home",
        GrootDestination.Lift => "Lift",
        GrootDestination.Run => "Run",
        GrootDestination.Progress => "Progress",
        _ => throw new ArgumentOutOfRangeException(nameof(dest)),
    };
}
