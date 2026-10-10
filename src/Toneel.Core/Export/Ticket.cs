using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Export;

/// <summary>The three lines printed on a ticket, and the name of the image file.</summary>
public sealed record Ticket(string Date, string Location, string Name, string FileName)
{
    /// <summary>Returns <c>null</c> for a reservation without a date: there is nothing to print yet.</summary>
    public static Ticket? For(Reservation reservation)
    {
        if (reservation.Date is not { } date) return null;

        return new Ticket(
            Date: DutchDate.Format(date),
            Location: reservation.Location,
            Name: $"{reservation.LastName} {reservation.FirstName}",
            FileName: $"{FileSafe(reservation.LastName)}_{FileSafe(reservation.FirstName)}-Ticket-Rare_Wereld.png");
    }

    /// <summary>Removes characters that Windows, macOS or Linux do not allow in a file name.</summary>
    public static string FileSafe(string text)
    {
        var cleaned = new string(
            [.. text.Trim().Select(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c) ? '_' : c)])
            .Replace(' ', '_')
            .Trim('.', '_');

        return cleaned.Length == 0 ? "ticket" : cleaned;
    }
}
