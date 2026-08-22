using Groot.Core.Intervals;
using Groot.UI.Audio;
using Microsoft.Maui.Media;

namespace Groot.App.Audio;

/// <summary>
/// On-device cue player for the MAUI head: the platform TextToSpeech voice, offline and
/// locale-matched. Android ToneGenerator beeps land here once the API-36 binding signature is
/// confirmed (Plan/android-foreground-rest-timer.md); the voice is the cue until then.
/// </summary>
public sealed class MauiCuePlayer : ICuePlayer
{
    private Locale? _voice;
    private string _language = RunCueText.DefaultLanguage;

    /// <summary>
    /// Cue language, as the two-letter code the programs use. Setting it drops the cached voice so
    /// the next cue picks one that matches.
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;

            _language = value;
            _voice = null;
        }
    }

    public async ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SpeechOptions { Volume = 1f, Locale = await VoiceAsync() };
            await TextToSpeech.Default.SpeakAsync(text, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The session moved on. A cue that is no longer current is not worth speaking.
        }
    }

    public ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// The first installed voice whose language matches, cached until the language changes. Null
    /// means the device has no voice for it, and TextToSpeech falls back to the system default:
    /// wrong accent, right words, which beats silence.
    /// </summary>
    private async ValueTask<Locale?> VoiceAsync()
    {
        if (_voice is not null) return _voice;

        var locales = await TextToSpeech.Default.GetLocalesAsync();

        _voice = locales.FirstOrDefault(l =>
            l.Language.StartsWith(_language, StringComparison.OrdinalIgnoreCase));

        return _voice;
    }
}
