using Groot.UI.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Groot.Web.Audio;

/// <summary>
/// Browser cue player: Web Speech API for the voice, WebAudio oscillators for the beeps.
/// A device with no voice stays silent on purpose, because a missing voice must never stop a
/// running session. A missing script is a different thing, and says so once.
/// </summary>
public sealed class WebCuePlayer(IJSRuntime js, ILogger<WebCuePlayer> log) : ICuePlayer, IAsyncDisposable
{
    private IJSObjectReference? _module;

    // Set when the module could not be imported. Without it every cue retries the import, which
    // during a run means one failed round trip per segment boundary.
    private bool _unavailable;

    public string Language { get; set; } = "en-GB";

    public async ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default) =>
        await InvokeAsync("speak", cancellationToken, text, Language);

    public async ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default) =>
        await InvokeAsync("beep", cancellationToken, sound.ToString());

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;

        var module = _module;
        _module = null;

        try
        {
            await module.InvokeVoidAsync("silence");
        }
        catch (JSDisconnectedException)
        {
            // The page went away first, which also stops the speech.
        }
        catch (JSException)
        {
            // Whatever the script did, the reference below still has to be released.
        }

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async ValueTask InvokeAsync(string function, CancellationToken cancellationToken, params object?[] args)
    {
        var module = await ModuleAsync(cancellationToken);
        if (module is null) return;

        try
        {
            await module.InvokeVoidAsync(function, cancellationToken, args);
        }
        catch (JSException error)
        {
            // No speech synthesis or no audio device. The screen still shows the segment, and the
            // reason is in the console rather than nowhere.
            log.LogWarning(error, "Cue {Function} did not play.", function);
        }
        catch (JSDisconnectedException)
        {
            // The circuit closed mid-cue.
        }
        catch (OperationCanceledException)
        {
            // The segment moved on.
        }
    }

    /// <summary>
    /// The cue module, imported once. A failed import is remembered: it means the script is not
    /// deployed, which no number of retries fixes, and retrying costs a round trip per cue.
    /// </summary>
    private async ValueTask<IJSObjectReference?> ModuleAsync(CancellationToken cancellationToken)
    {
        if (_module is not null || _unavailable) return _module;

        try
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/cues.js");
        }
        catch (JSException error)
        {
            _unavailable = true;
            log.LogError(error, "js/cues.js could not be imported; this session runs without cues.");
        }
        catch (JSDisconnectedException)
        {
            _unavailable = true;
        }
        catch (OperationCanceledException)
        {
        }

        return _module;
    }
}
