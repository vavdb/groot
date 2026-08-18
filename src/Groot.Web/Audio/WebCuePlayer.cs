using Groot.UI.Audio;
using Microsoft.JSInterop;

namespace Groot.Web.Audio;

/// <summary>
/// Browser cue player: Web Speech API for the voice, WebAudio oscillators for the beeps.
/// Failures stay silent on purpose — a missing voice must never stop a running session.
/// </summary>
public sealed class WebCuePlayer(IJSRuntime js) : ICuePlayer, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public string Language { get; set; } = "en-GB";

    public async ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default) =>
        await InvokeAsync("speak", cancellationToken, text, Language);

    public async ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default) =>
        await InvokeAsync("beep", cancellationToken, sound.ToString());

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;

        try
        {
            await _module.InvokeVoidAsync("silence");
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // the page went away first
        }
        finally
        {
            _module = null;
        }
    }

    private async ValueTask InvokeAsync(string function, CancellationToken cancellationToken, params object?[] args)
    {
        try
        {
            var module = await ModuleAsync(cancellationToken);
            await module.InvokeVoidAsync(function, cancellationToken, args);
        }
        catch (JSException)
        {
            // no speech synthesis or no audio device: the screen still shows the segment
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async ValueTask<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken) =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/cues.js");
}
