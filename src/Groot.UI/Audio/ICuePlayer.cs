namespace Groot.UI.Audio;

/// <summary>The beep that precedes a spoken cue, so the runner knows what happened without listening.</summary>
public enum CueSound { RunStart, WalkStart, Warning, Finish }

/// <summary>
/// The only platform seam in Groot.UI: heads speak cues their own way (Web Speech API,
/// Android TextToSpeech, AVSpeechSynthesizer). Components never touch audio directly.
/// </summary>
public interface ICuePlayer
{
    ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default);

    ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default);
}

/// <summary>Does nothing, audibly. The default when a head has no voice wired up yet.</summary>
public sealed class SilentCuePlayer : ICuePlayer
{
    public ValueTask SpeakAsync(string text, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask PlayAsync(CueSound sound, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
