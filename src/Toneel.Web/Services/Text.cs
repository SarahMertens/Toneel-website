using System.Globalization;

namespace Toneel.Web.Services;

/// <summary>Small helpers for Dutch wording.</summary>
public static class Text
{
    /// <summary>"1 plaats", "3 plaatsen"</summary>
    public static string Count(int number, string singular, string plural) =>
        $"{number} {(number == 1 ? singular : plural)}";

    public static string Seats(int number) => Count(number, "plaats", "plaatsen");

    public static string Reservations(int number) => Count(number, "reservatie", "reservaties");

    /// <summary>"€496" or "€12,50"</summary>
    public static string Euro(decimal amount) =>
        "€" + amount.ToString(amount == decimal.Truncate(amount) ? "0" : "0.00", DutchNumbers);

    private static readonly NumberFormatInfo DutchNumbers = new() { NumberDecimalSeparator = "," };
}
