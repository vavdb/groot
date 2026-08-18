namespace Groot.Core.Intervals;

/// <summary>
/// Renders a <see cref="RunCue"/> into a spoken sentence. A stand-in for the i18n resource
/// pipeline: English defaults with a minimal Dutch fallback, no external dependency.
/// </summary>
public static class RunCueText
{
    public const string DefaultLanguage = "en";

    public static string Speak(RunCue cue, string language = DefaultLanguage) =>
        IsDutch(language) ? Dutch(cue) : English(cue);

    private static bool IsDutch(string language) =>
        language.StartsWith("nl", StringComparison.OrdinalIgnoreCase);

    private static string English(RunCue cue) => cue.Key switch
    {
        "cue.startRun" => $"Run, {Duration(cue.SegmentSeconds, "en")}.",
        "cue.startWalk" => cue.SegmentLabel switch
        {
            "warmup" => $"Warm-up walk, {Duration(cue.SegmentSeconds, "en")}.",
            "cooldown" => $"Cool down. Walk it out for {Duration(cue.SegmentSeconds, "en")}.",
            _ => $"Walk, {Duration(cue.SegmentSeconds, "en")}. Breathe easy.",
        },
        "cue.endingSoon" => cue.NextKind switch
        {
            SegmentKind.Run => $"{Duration(cue.RemainingInSegment, "en")} to go. Get ready to run.",
            SegmentKind.Walk => $"{Duration(cue.RemainingInSegment, "en")} to go, then walk.",
            _ => $"{Duration(cue.RemainingInSegment, "en")} to go.",
        },
        "cue.warmupEnding" => $"Almost done with the warm-up. Get ready for your {Ordinal(cue.NextRunOrdinal, "en")} run.",
        "cue.halfway" => $"Halfway. {Duration(cue.RemainingInSegment, "en")} left.",
        "cue.finalMinute" => "Final minute. Hold your pace.",
        "cue.finished" => "Session complete. Nice work.",
        _ => $"{Duration(cue.RemainingInSegment, "en")} left in this {Kind(cue.Kind, "en")}.",
    };

    private static string Dutch(RunCue cue) => cue.Key switch
    {
        "cue.startRun" => $"Rennen, {Duration(cue.SegmentSeconds, "nl")}.",
        "cue.startWalk" => cue.SegmentLabel switch
        {
            "warmup" => $"Warming-up wandelen, {Duration(cue.SegmentSeconds, "nl")}.",
            "cooldown" => $"Cooling-down. Wandel nog {Duration(cue.SegmentSeconds, "nl")}.",
            _ => $"Wandelen, {Duration(cue.SegmentSeconds, "nl")}. Adem rustig.",
        },
        "cue.endingSoon" => cue.NextKind switch
        {
            SegmentKind.Run => $"Nog {Duration(cue.RemainingInSegment, "nl")}. Maak je klaar om te rennen.",
            SegmentKind.Walk => $"Nog {Duration(cue.RemainingInSegment, "nl")}, dan rustig wandelen.",
            _ => $"Nog {Duration(cue.RemainingInSegment, "nl")}.",
        },
        "cue.warmupEnding" => $"Bijna klaar met de warming-up. Maak je klaar voor je {Ordinal(cue.NextRunOrdinal, "nl")} ren-blok.",
        "cue.halfway" => $"Halverwege. Nog {Duration(cue.RemainingInSegment, "nl")}.",
        "cue.finalMinute" => "Laatste minuut. Hou je tempo vast.",
        "cue.finished" => "Sessie klaar. Goed gedaan.",
        _ => $"Nog {Duration(cue.RemainingInSegment, "nl")} in dit {Kind(cue.Kind, "nl")}-blok.",
    };

    private static string Kind(SegmentKind kind, string language) => (kind, IsDutch(language)) switch
    {
        (SegmentKind.Run, false) => "run",
        (SegmentKind.Walk, false) => "walk",
        (SegmentKind.Run, true) => "ren",
        _ => "wandel",
    };

    /// <summary>Spoken duration: "ninety seconds" reads worse than "1 minute 30" for a TTS voice.</summary>
    public static string Duration(int seconds, string language = DefaultLanguage)
    {
        var dutch = IsDutch(language);
        var minutes = seconds / 60;
        var rest = seconds % 60;

        if (minutes == 0) return $"{rest} {(dutch ? "seconden" : "seconds")}";

        var minuteWord = minutes == 1 ? (dutch ? "minuut" : "minute") : (dutch ? "minuten" : "minutes");
        return rest == 0
            ? $"{minutes} {minuteWord}"
            : $"{minutes} {minuteWord} {rest}";
    }

    private static string Ordinal(int value, string language)
    {
        var dutch = IsDutch(language);
        return value switch
        {
            1 => dutch ? "eerste" : "first",
            2 => dutch ? "tweede" : "second",
            3 => dutch ? "derde" : "third",
            4 => dutch ? "vierde" : "fourth",
            5 => dutch ? "vijfde" : "fifth",
            6 => dutch ? "zesde" : "sixth",
            7 => dutch ? "zevende" : "seventh",
            8 => dutch ? "achtste" : "eighth",
            9 => dutch ? "negende" : "ninth",
            _ => dutch ? $"{value}e" : $"{value}th",
        };
    }
}
