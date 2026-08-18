namespace Groot.Core.Intervals;

/// <summary>Why a cue fired: from the program definition, or added by the engine.</summary>
public enum RunCueKind { SegmentStart, Declared, EndingSoon, Finished }

/// <summary>
/// A cue with everything a platform needs to speak it without knowing the program:
/// which segment it belongs to, what comes next, and how much is left.
/// </summary>
public sealed record RunCue(
    string Key,
    RunCueKind CueKind,
    int AtSecond,
    int SegmentIndex,
    SegmentKind Kind,
    string? SegmentLabel,
    int SegmentSeconds,
    SegmentKind? NextKind,
    int? NextSeconds,
    int NextRunOrdinal,
    int RemainingInSegment,
    int RemainingTotal);
