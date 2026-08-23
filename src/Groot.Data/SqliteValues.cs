using System.Globalization;

namespace Groot.Data;

/// <summary>
/// Conversions between the domain's types and the three storage types SQLite actually has.
/// Every row record holds strings, longs and ints only, so nothing depends on a provider or a
/// Dapper type handler guessing right: reading a value back is the inverse of writing it, here.
/// </summary>
internal static class SqliteValues
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>Decimals are TEXT: SQLite's REAL would store 42.5 kg as 42.499999999999996.</summary>
    public static string FromDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static string? FromDecimal(decimal? value) => value is { } kg ? FromDecimal(kg) : null;

    public static decimal ToDecimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    public static decimal? ToDecimalOrNull(string? value) => value is null ? null : ToDecimal(value);

    /// <summary>
    /// Plate weights, in grams. Every other weight is a TEXT decimal, but a plate weight is part
    /// of a primary key, and '5' and '5.0' are two keys for one plate.
    /// </summary>
    public static long FromKilograms(decimal kilograms) => (long)Math.Round(kilograms * 1000m);

    public static decimal ToKilograms(long grams) => grams / 1000m;

    /// <summary>Calendar days, not instants: a session logged at 23:50 belongs to that day.</summary>
    public static string FromDate(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static DateOnly ToDate(string value) =>
        DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);

    public static string FromGuid(Guid id) => id.ToString("D");

    public static Guid ToGuid(string value) => Guid.ParseExact(value, "D");

    public static long FromBool(bool value) => value ? 1 : 0;

    public static bool ToBool(long value) => value != 0;

    /// <summary>Enums are stored by name so a database dump reads like the domain.</summary>
    public static string FromEnum<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();

    /// <summary>
    /// Parses by name only. <see cref="Enum.Parse{TEnum}(string)"/> also accepts a number, so a
    /// row holding "9999" would come back as an undefined value of the enum rather than throwing.
    /// </summary>
    public static TEnum ToEnum<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new InvalidOperationException($"Stored value '{value}' is not a {typeof(TEnum).Name}.");
}
