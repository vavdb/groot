using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Groot.UI.Theme;
using Microsoft.AspNetCore.Components;

namespace Groot.UI.Tests;

/// <summary>
/// The phone head navigates from the bottom nav and nowhere else, so the shell wiring behind it is
/// the difference between an app you can move around and two screens you can only reach by link.
/// </summary>
public sealed class ShellNavigationTests : BunitContext, IAsyncLifetime
{
    private static readonly GrootDestination[] Phone =
        [GrootDestination.Run, GrootDestination.Home, GrootDestination.Lift];

    public ShellNavigationTests()
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

    private IRenderedComponent<GrootShell> RenderShell(IReadOnlyList<GrootDestination>? destinations) =>
        Render<GrootShell>(p => p
            .Add(x => x.BottomNavDestinations, destinations)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "body"))));

    [Fact]
    public void A_shell_without_destinations_has_no_bottom_nav()
    {
        // The desktop web head navigates from the drawer; a second nav would be two answers.
        var cut = RenderShell(destinations: null);

        Assert.Empty(cut.FindAll(".bottom-nav"));
    }

    [Fact]
    public void A_shell_shows_only_the_destinations_it_was_given()
    {
        var cut = RenderShell(Phone);

        // Progress has no screen yet, so it is not offered.
        Assert.Equal(3, cut.FindAll(".bottom-nav .item").Count);
        Assert.DoesNotContain("Progress", cut.Find(".bottom-nav").TextContent);
    }

    [Theory]
    [InlineData(0, "run")]
    [InlineData(1, "")]
    [InlineData(2, "lift")]
    public void Tapping_a_tab_navigates_to_its_screen(int tab, string expected)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderShell(Phone);

        cut.FindAll(".bottom-nav .item")[tab].Click();

        Assert.Equal(expected, navigation.ToBaseRelativePath(navigation.Uri));
    }

    [Fact]
    public void The_tab_for_the_current_address_reads_as_current()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/lift");

        var cut = RenderShell(Phone);

        var active = cut.Find(".bottom-nav .item.active");
        Assert.Contains("Lift", active.TextContent);
    }

    [Fact]
    public void A_tab_can_be_reached_and_activated_from_the_keyboard()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderShell(Phone);

        var run = cut.FindAll(".bottom-nav .item")[0];
        Assert.Equal("0", run.GetAttribute("tabindex"));

        run.KeyDown(key: "Enter");

        Assert.Equal("run", navigation.ToBaseRelativePath(navigation.Uri));
    }
}
