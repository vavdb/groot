namespace Groot.UI.Gallery.Pages;

/// <summary>
/// One year of training for the Progress concepts: the kilograms moved, and the weeks that need a
/// mark of their own. Sample data, not a store read, so the concepts can be compared before the
/// Progress screen has anything real behind it.
/// </summary>
public sealed record YearLoad(int Year, int Kilos, bool HasGap = false, bool HasJoker = false, bool HasPersonalRecord = false)
{
    /// <summary>
    /// The volume split into plate denominations the rack actually owns. This is a way of drawing
    /// a number, not a bar anyone could load: a year is thousands of lifts, and PlateSolver
    /// answers a different question (what goes on the bar for one set).
    /// </summary>
    public IReadOnlyList<int> Plates
    {
        get
        {
            var units = (int)Math.Round(Kilos / 4000.0);
            var plates = new List<int>();

            foreach (var denomination in Denominations)
            {
                while (units >= denomination)
                {
                    plates.Add(denomination);
                    units -= denomination;
                }
            }

            if (units > 0) plates.Add(Denominations[^1]);
            return plates;
        }
    }

    private static readonly int[] Denominations = [25, 20, 15, 10, 5];

    /// <summary>Five years with a thin one, a strong one, a gap, a joker and a record in them.</summary>
    public static IReadOnlyList<YearLoad> Sample { get; } =
    [
        new(2023, 118_000),
        new(2024, 214_000),
        new(2025, 96_000, HasGap: true),
        new(2026, 262_000, HasJoker: true),
        new(2027, 341_000, HasPersonalRecord: true),
    ];

    /// <summary>The heaviest year in a set, so every concept can share one scale.</summary>
    public static int Peak(IReadOnlyList<YearLoad> years) => years.Max(y => y.Kilos);
}
