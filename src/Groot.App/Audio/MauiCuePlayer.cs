using Groot.UI.Audio;
using Microsoft.Maui.Media;

namespace Groot.App.Audio;

/// <summary>
/// On-device cue player for the MAUI head: platform TextToSpeech voice (offline,
/// locale-matched). Android ToneGenerator beeps land here once the API-36 binding
/// signature is confirmed — the voice itself is the primary cue for now.
/// </summary>
public sealed class MauiCuePlayer : ICuePlayer
{
    public async ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Volume = 1f });
    }

    public ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default)
    {
        // TODO: Android ToneGenerator beep (audio-focus ducking) — see design/habit-system.md §3.2.
        return ValueTask.CompletedTask;
    }
}
