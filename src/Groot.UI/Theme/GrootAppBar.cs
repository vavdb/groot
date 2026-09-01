using Microsoft.AspNetCore.Components;

namespace Groot.UI.Theme;

/// <summary>
/// The app bar's title area, handed down to whatever screen is open so the screen can put its own
/// heading there instead of repeating it inside the page. A head that has an app bar cascades one
/// of these; the gallery does not, which is why a screen rendered there keeps its heading inline.
/// </summary>
public sealed class GrootAppBar
{
    private object? _owner;

    /// <summary>What the open screen wants in the bar, or null for the head's own brand.</summary>
    public RenderFragment? Content { get; private set; }

    /// <summary>Raised when the content changes, so the shell can render again.</summary>
    public event Action? Changed;

    /// <summary>
    /// Puts a screen's heading in the bar. The screen passes itself, because a head can keep a
    /// screen mounted behind another one (the phone head does that with a run in progress) and
    /// only the screen that put a heading there is allowed to take it away again.
    /// </summary>
    public void Set(object owner, RenderFragment content)
    {
        _owner = owner;
        Content = content;
        Changed?.Invoke();
    }

    /// <summary>Takes this screen's heading back out, which restores the head's brand.</summary>
    public void Clear(object owner)
    {
        if (!ReferenceEquals(_owner, owner)) return;

        _owner = null;
        Content = null;
        Changed?.Invoke();
    }
}
