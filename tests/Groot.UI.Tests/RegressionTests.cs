using System.Reflection;
using Bunit;
using Groot.Core.Programs;
using Groot.UI.Components;
using Groot.UI.Theme;
using Microsoft.AspNetCore.Components;

namespace Groot.UI.Tests;

/// <summary>
/// Guards against three render-only bugs found 2026-08-21 (fixed in a50d954) — none of them
/// failed <c>dotnet build</c>, all three only broke at runtime. See Plan/bunit-smoke-tests.md.
/// </summary>
public sealed class RegressionTests : BunitContext, IAsyncLifetime
{
    public RegressionTests()
    {
        Services.AddGrootUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // MudBlazor registers services (popover, pointer-events) that are IAsyncDisposable-only;
    // xUnit's default sync Dispose() on BunitContext trips over that. Opting into IAsyncLifetime
    // makes xUnit tear down via DisposeAsync instead.
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WebHomePage_HasRootRoute()
    {
        // Home.razor lost its @page "/" directive during a refactor; the root route 404'd on
        // both heads. A missing @page attribute compiles fine, so this can only be caught by
        // inspecting the compiled RouteAttribute (or, in the real app, by actually navigating).
        var homeType = typeof(Groot.Web.Pages.Home);
        var routes = homeType.GetCustomAttributes<RouteAttribute>().Select(r => r.Template);

        Assert.Contains("/", routes);
    }

    [Fact]
    public void GrootShell_IncludesPopoverProvider()
    {
        // Without <MudPopoverProvider /> every MudSelect (Program/Week/Session/Voice) is inert —
        // no container to render the popover into. No exception is thrown; the dropdown just
        // never opens. Assert the provider is present rather than relying on a click-driven test.
        var cut = Render<GrootShell>(p => p
            .Add(x => x.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        Assert.NotEmpty(cut.FindAll(".mud-popover-provider"));
    }

    [Fact]
    public void GrootRunScene_AudioControlsBindingDoesNotThrow()
    {
        // GrootRunScene used @bind-Language/@bind-Sound against GrootAudioControls, which never
        // declared the LanguageChanged/SoundChanged EventCallback parameters that binding syntax
        // requires — Blazor threw ThrowForUnknownIncomingParameterName and killed the whole
        // render tree. A missing bind-target callback is a runtime failure, not a compile error.
        var programs = ProgramCatalog.Embedded.IntervalPrograms;

        var render = () => Render<GrootRunScene>(p => p
            .Add(x => x.Programs, programs)
            .Add(x => x.ShowAudioControls, true));

        var cut = Record.Exception(render);
        Assert.Null(cut);
    }

    [Fact]
    public void GrootAudioControls_ChangingLanguageRaisesLanguageChanged()
    {
        // MudSelect's dropdown renders into a MudPopoverProvider elsewhere in the tree (the real
        // app gets one from GrootShell) — not under GrootAudioControls' own render output, so
        // the popover's list items have to be found and clicked via `provider`, not `cut`.
        var provider = Render<MudBlazor.MudPopoverProvider>();

        string? changedTo = null;
        var cut = Render<GrootAudioControls>(p => p
            .Add(x => x.Language, "en")
            .Add(x => x.LanguageChanged, lang => changedTo = lang));

        // MudSelect opens on mousedown against its input control, not a plain click.
        cut.Find(".mud-input-control").MouseDown();
        provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-list-item")));
        provider.FindAll(".mud-list-item")[1].Click(); // [0] English, [1] Nederlands

        Assert.Equal("nl", changedTo);
    }
}
