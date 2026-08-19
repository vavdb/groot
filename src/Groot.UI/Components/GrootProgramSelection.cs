using Groot.Core.Intervals;

namespace Groot.UI.Components;

/// <summary>What the picker selected: the resolved session plus how many sessions the week holds.</summary>
public sealed record GrootProgramSelection(RunSession Session, int SessionsInWeek);
