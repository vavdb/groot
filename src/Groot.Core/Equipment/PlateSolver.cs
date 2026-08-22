namespace Groot.Core.Equipment;

/// <summary>A plate denomination the user owns, with a pair count (plates load in pairs).</summary>
public sealed record PlatePair(decimal Kg, int Pairs);

/// <summary>
/// Computes achievable bar loads and per-side breakdowns from a plate inventory.
/// Targets round to achievable loads; the progression engine never proposes a weight
/// the rack cannot build.
/// </summary>
public static class PlateSolver
{
    /// <summary>All achievable totals for a bar + inventory, ascending, nominal (counts-as) weights.</summary>
    public static IReadOnlyList<decimal> AchievableTotals(Equipment bar, IReadOnlyList<PlatePair> inventory)
    {
        var perSideSums = new HashSet<decimal> { 0m };
        foreach (var (kg, pairs) in inventory.Select(p => (p.Kg, p.Pairs)))
        {
            var current = perSideSums.ToArray();
            foreach (var baseSum in current)
                for (var n = 1; n <= pairs; n++)
                    perSideSums.Add(baseSum + kg * n);
        }

        return perSideSums
            .Select(side => bar.EffectiveBarKg + 2m * side)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();
    }

    /// <summary>Nearest achievable total at or above <paramref name="targetKg"/>; falls back to nearest below; null when inventory is empty.</summary>
    public static decimal? RoundToAchievable(decimal targetKg, IReadOnlyList<decimal> achievableTotals)
    {
        if (achievableTotals.Count == 0) return null;
        var atOrAbove = achievableTotals.Where(t => t >= targetKg).ToArray();
        return atOrAbove.Length > 0 ? atOrAbove.Min() : achievableTotals.Max();
    }

    /// <summary>
    /// Per-side breakdown for a total, heaviest plate first; null when the inventory cannot build
    /// it. Every total <see cref="AchievableTotals"/> reports is buildable here: a greedy pass
    /// alone breaks that promise, because taking the heaviest plate that fits can strand the
    /// remainder (4 + 3 + 3 loads 10 a side, greedy takes the 4 first and cannot finish 6 with 3s).
    /// So the pass backtracks.
    /// </summary>
    public static IReadOnlyList<decimal>? PerSideBreakdown(decimal totalKg, Equipment bar, IReadOnlyList<PlatePair> inventory)
    {
        var perSide = (totalKg - bar.EffectiveBarKg) / 2m;
        if (perSide < 0m) return null;
        if (perSide == 0m) return Array.Empty<decimal>();

        var plates = inventory.Where(p => p.Kg > 0m && p.Pairs > 0).OrderByDescending(p => p.Kg).ToArray();
        var chosen = new List<decimal>();

        return Load(plates, 0, perSide, chosen) ? chosen : null;
    }

    /// <summary>
    /// Depth-first over the denominations, most plates of the heaviest first, so the first
    /// solution found is also the one with the fewest plates to lift.
    /// </summary>
    private static bool Load(PlatePair[] plates, int index, decimal remaining, List<decimal> chosen)
    {
        if (remaining == 0m) return true;
        if (index >= plates.Length) return false;

        var (kg, pairs) = (plates[index].Kg, plates[index].Pairs);
        var most = (int)Math.Min(pairs, Math.Floor(remaining / kg));

        for (var count = most; count >= 0; count--)
        {
            for (var i = 0; i < count; i++) chosen.Add(kg);

            if (Load(plates, index + 1, remaining - count * kg, chosen)) return true;

            chosen.RemoveRange(chosen.Count - count, count);
        }

        return false;
    }
}
