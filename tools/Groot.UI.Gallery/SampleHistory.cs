using Groot.UI.Components;

namespace Groot.UI.Gallery;

/// <summary>
/// Plausible training history for the gallery: four sessions most weeks, three lean weeks for a
/// trip, a cold and a deload, and the odd double day. Deterministic, so every render of the
/// gallery shows the same six months.
/// </summary>
public static class SampleHistory
{
    public static SeasonDay[] Weeks(DateOnly today, int weeks = 26)
    {
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)).AddDays(-7 * (weeks - 1));
        var days = new List<SeasonDay>();

        for (var week = 0; week < weeks; week++)
        {
            var lean = week is 7 or 8 or 19;
            var planned = lean ? 1 : week % 5 == 0 ? 5 : 4;

            // Rotated so the pattern does not stripe down the grid.
            var offsets = new[] { 0, 2, 4, 5, 1, 3 }.Skip(week % 3).Take(planned);

            foreach (var offset in offsets)
            {
                var date = monday.AddDays(week * 7 + offset);
                if (date > today) continue;

                days.Add(new SeasonDay(date, (week * 7 + offset) % 17 == 0 ? 2 : 1));
            }
        }

        return days.ToArray();
    }
}
