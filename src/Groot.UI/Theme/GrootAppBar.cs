using Microsoft.AspNetCore.Components;

namespace Groot.UI.Theme;

/// <summary>
/// The app bar's title area, handed down to whatever screen is open so the screen can put its own
/// heading there instead of repeating it inside the page. A head that has an app bar cascades one
/// of these; the gallery does not, which is why a screen rendered there keeps its heading inline.
/// </summary>
public sealed class GrootAppBar
{
    /// <summary>What the open screen wants in the bar, or null for the head's own brand.</summary>
    public RenderFragment? Content { get; private set; }

    /// <summary>Raised when the content changes, so the shell can render again.</summary>
    public event Action? Changed;

    /// <summary>Puts a screen's heading in the bar. Null on the way out, which restores the brand.</summary>
    public void Set(RenderFragment? content)
    {
        Content = content;
        Changed?.Invoke();
    }
}
