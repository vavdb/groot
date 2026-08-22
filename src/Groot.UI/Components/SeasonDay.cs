namespace Groot.UI.Components;

/// <summary>One day of training history: the date and how many sessions were logged on it.</summary>
public sealed record SeasonDay(DateOnly Date, int Sessions);
