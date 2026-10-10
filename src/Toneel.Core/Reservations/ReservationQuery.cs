using Toneel.Core.Schedule;

namespace Toneel.Core.Reservations;

/// <summary>Which reservations to show: a free-text search and an optional date filter.</summary>
public readonly record struct ReservationFilter(string Search = "", DateFilter Date = default)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Search) && Date.Kind == DateFilterKind.All;
}

public enum DateFilterKind
{
    /// <summary>Every reservation.</summary>
    All,

    /// <summary>Only reservations for one performance.</summary>
    On,

    /// <summary>Only reservations for which no date was chosen yet.</summary>
    Undated,
}

public readonly record struct DateFilter(DateFilterKind Kind, DateOnly Date = default)
{
    public static DateFilter All => default;

    public static DateFilter Undated => new(DateFilterKind.Undated);

    public static DateFilter On(DateOnly date) => new(DateFilterKind.On, date);

    public bool Matches(DateOnly? date) => Kind switch
    {
        DateFilterKind.On => date == Date,
        DateFilterKind.Undated => date is null,
        _ => true,
    };
}

public static class ReservationQuery
{
    /// <summary>The filtered reservations, newest first.</summary>
    public static IReadOnlyList<Reservation> Apply(
        IEnumerable<Reservation> reservations,
        ReservationFilter filter)
    {
        // Every word must appear somewhere, so "jan boeie" finds Jan at De Boeie.
        // `default(ReservationFilter)` leaves Search null, hence the fallback.
        var words = (filter.Search ?? "").Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return
        [
            .. reservations
                .Where(reservation => filter.Date.Matches(reservation.Date))
                .Where(reservation =>
                {
                    var text = SearchText(reservation);
                    return words.All(word =>
                        text.Contains(word, StringComparison.CurrentCultureIgnoreCase));
                })
                .OrderByDescending(reservation => reservation.AddedAt),
        ];
    }

    private static string SearchText(Reservation reservation) =>
        string.Join(
            ' ',
            reservation.FirstName,
            reservation.LastName,
            reservation.Date is { } date ? DutchDate.Format(date) : "nog niet gekozen",
            reservation.Location);
}
