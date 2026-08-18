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

    /// <summary>Greedy per-side breakdown for a total; null when the total is not buildable.</summary>
    public static IReadOnlyList<decimal>? PerSideBreakdown(decimal totalKg, Equipment bar, IReadOnlyList<PlatePair> inventory)
    {
        var perSide = (totalKg - bar.EffectiveBarKg) / 2m;
        if (perSide < 0m) return null;
        if (perSide == 0m) return Array.Empty<decimal>();

        var result = new List<decimal>();
        var remaining = perSide;
        foreach (var plate in inventory.OrderByDescending(p => p.Kg))
        {
            var used = 0;
            while (used < plate.Pairs && remaining >= plate.Kg)
            {
                result.Add(plate.Kg);
                remaining -= plate.Kg;
                used++;
            }
        }

        return remaining == 0m ? result : null;
    }
}
