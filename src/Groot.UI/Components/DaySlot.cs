namespace Groot.UI.Components;

public enum DaySlotVisual { Pending, LiftDone, RunDone, Joker, Today, Rest }

/// <summary>One day cell on the week card: label (MO..SU), icon, caption, visual state.</summary>
public sealed record DaySlot(string DayLabel, string Icon, string Caption, DaySlotVisual Visual);
