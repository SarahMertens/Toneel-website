using Toneel.Core.Schedule;

namespace Toneel.Core.Reservations;

/// <summary>Seats booked for one performance.</summary>
public sealed record PerformanceCount(Performance Performance, int Seats);

/// <summary>Totals over a set of reservations.</summary>
public sealed record ReservationStats
{
    public required int Reservations { get; init; }

    public required int Seats { get; init; }

    /// <summary>Seats times the ticket price, in euro.</summary>
    public required decimal Revenue { get; init; }

    /// <summary>One entry per performance, in chronological order, zero included.</summary>
    public required IReadOnlyList<PerformanceCount> PerPerformance { get; init; }

    /// <summary>Seats of reservations that have no date yet.</summary>
    public required int UndatedSeats { get; init; }

    public static ReservationStats Of(IEnumerable<Reservation> reservations, Production production)
    {
        var list = reservations.ToList();
        var seatsByDate = list
            .Where(reservation => production.Find(reservation.Date) is not null)
            .GroupBy(reservation => reservation.Date!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(reservation => reservation.Seats));

        var seats = list.Sum(reservation => reservation.Seats);

        return new ReservationStats
        {
            Reservations = list.Count,
            Seats = seats,
            Revenue = seats * production.TicketPrice,
            PerPerformance =
            [
                .. production.Performances.Select(performance =>
                    new PerformanceCount(performance, seatsByDate.GetValueOrDefault(performance.Date))),
            ],
            // Also counts dates that are not (or no longer) on the schedule.
            UndatedSeats = seats - seatsByDate.Values.Sum(),
        };
    }
}
