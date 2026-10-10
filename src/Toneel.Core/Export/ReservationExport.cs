using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Export;

/// <summary>A file the browser can offer as a download.</summary>
public sealed record ExportFile(string FileName, string ContentType, byte[] Content);

public static class ReservationExport
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string NoDate = "Nog niet gekozen";

    /// <summary>
    /// The reservation list as handed to the venues: the total amount on top,
    /// one numbered row per reservation (oldest first), and seats per date below.
    /// </summary>
    public static ExportFile ToExcel(IEnumerable<Reservation> reservations, Production production)
    {
        var rows = reservations.OrderBy(reservation => reservation.AddedAt).ToList();
        var stats = ReservationStats.Of(rows, production);
        var name = production.Company.Replace(' ', '_');

        var sheet = new XlsxWriter(name)
            .ColumnWidths(10, 18, 28, 20, 20, 10)
            .Set(1, 1, $"Totaal bedrag: €{stats.Revenue:0.##}")
            .Merge(1, firstColumn: 1, lastColumn: 6)
            .SetRow(3, 1, "Nummer", "Datum", "Locatie", "Naam", "Voornaam", "Aantal");

        var row = 4;
        foreach (var reservation in rows)
        {
            sheet.SetRow(
                row,
                1,
                (row - 3).ToString("000"),
                reservation.Date is { } date ? DutchDate.Format(date) : NoDate,
                reservation.Location,
                reservation.LastName,
                reservation.FirstName,
                reservation.Seats);
            row++;
        }

        row += 2;
        sheet.Set(row++, 1, "Aantal personen per datum:");

        foreach (var count in stats.PerPerformance.Where(count => count.Seats > 0))
        {
            sheet.SetRow(row++, 1, DutchDate.Format(count.Performance.Date), count.Seats);
        }

        if (stats.UndatedSeats > 0)
        {
            sheet.SetRow(row, 1, NoDate, stats.UndatedSeats);
        }

        return new ExportFile($"{name}.xlsx", XlsxContentType, sheet.ToArray());
    }
}
