using Toneel.Core.Reservations;

namespace Toneel.Core.Tests;

public class ReservationStatsTests
{
    [Fact]
    public void Counts_seats_not_reservations_and_multiplies_by_the_price()
    {
        var stats = ReservationStats.Of(
            [
                Sample.Reservation(seats: 2),
                Sample.Reservation("Jef", seats: 3),
                Sample.Reservation("Nora", date: Sample.Sunday, seats: 1),
            ],
            Sample.Production);

        Assert.Equal(3, stats.Reservations);
        Assert.Equal(6, stats.Seats);
        Assert.Equal(48m, stats.Revenue);
    }

    [Fact]
    public void Lists_every_performance_in_order_including_empty_ones()
    {
        var stats = ReservationStats.Of([Sample.Reservation(date: Sample.Sunday, seats: 4)], Sample.Production);

        Assert.Equal(2, stats.PerPerformance.Count);
        Assert.Equal(Sample.Saturday, stats.PerPerformance[0].Performance.Date);
        Assert.Equal(0, stats.PerPerformance[0].Seats);
        Assert.Equal(4, stats.PerPerformance[1].Seats);
    }

    [Fact]
    public void Counts_seats_without_a_known_performance_separately()
    {
        var stats = ReservationStats.Of(
            [
                Sample.Reservation(seats: 2),
                Sample.Reservation("Nora", undated: true, seats: 3),
                Sample.Reservation("Wout", seats: 1) with { Date = new DateOnly(2030, 1, 1) },
            ],
            Sample.Production);

        Assert.Equal(4, stats.UndatedSeats);
        Assert.Equal(6, stats.Seats);
    }

    [Fact]
    public void An_empty_list_gives_zeroes()
    {
        var stats = ReservationStats.Of([], Sample.Production);

        Assert.Equal(0, stats.Reservations);
        Assert.Equal(0, stats.Seats);
        Assert.Equal(0m, stats.Revenue);
        Assert.Equal(0, stats.UndatedSeats);
    }
}
