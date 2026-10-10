namespace Toneel.Core.Schedule;

/// <summary>
/// Reads and writes dates as "24 januari 2026".
/// </summary>
/// <remarks>
/// The database stores performance dates in this form. The month names are
/// listed here instead of taken from <c>CultureInfo</c>, because a Blazor
/// WebAssembly app only ships a subset of culture data and the result must
/// be identical on every machine.
/// </remarks>
public static class DutchDate
{
    private static readonly string[] Months =
    [
        "januari", "februari", "maart", "april", "mei", "juni",
        "juli", "augustus", "september", "oktober", "november", "december",
    ];

    private static readonly string[] Weekdays =
    [
        "zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag",
    ];

    /// <summary>"24 januari 2026"</summary>
    public static string Format(DateOnly date) =>
        $"{date.Day} {Months[date.Month - 1]} {date.Year}";

    /// <summary>"zaterdag 24 januari"</summary>
    public static string FormatWithWeekday(DateOnly date) =>
        $"{Weekdays[(int)date.DayOfWeek]} {date.Day} {Months[date.Month - 1]}";

    /// <summary>"za"</summary>
    public static string ShortWeekday(DateOnly date) => Weekdays[(int)date.DayOfWeek][..2];

    /// <summary>"jan"</summary>
    public static string ShortMonth(DateOnly date) => Months[date.Month - 1][..3];

    public static bool TryParse(string? text, out DateOnly date)
    {
        date = default;

        var parts = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;

        var month = Array.FindIndex(
            Months,
            name => name.Equals(parts[1], StringComparison.OrdinalIgnoreCase)) + 1;

        if (month == 0
            || !int.TryParse(parts[0], out var day)
            || !int.TryParse(parts[2], out var year)
            || year is < 1 or > 9999
            || day < 1
            || day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        date = new DateOnly(year, month, day);
        return true;
    }
}
