using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Tests;

public class ReservationJsonTests
{
    [Fact]
    public void A_list_survives_a_round_trip()
    {
        Reservation[] original =
        [
            Sample.Reservation("Lotte", id: "1", seats: 2),
            Sample.Reservation("Nora", id: "2", undated: true),
        ];

        var restored = ReservationJson.Deserialize(ReservationJson.Serialize(original));

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Count);
        Assert.Equal(original[0], restored[0]);
        Assert.Equal(original[1], restored[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{not json")]
    [InlineData("\"text\"")]
    [InlineData("{}")]
    public void Unreadable_text_gives_null_instead_of_an_error(string? json)
    {
        Assert.Null(ReservationJson.Deserialize(json));
    }
}

public class DemoDataTests
{
    [Fact]
    public void Demo_reservations_fit_the_production()
    {
        var production = Production.RareWereld;

        var demo = DemoData.For(production, FixedClock.Default);

        Assert.True(demo.Count >= 10);
        Assert.Contains(demo, reservation => reservation.Date is null);

        foreach (var reservation in demo)
        {
            Assert.InRange(reservation.Seats, Reservation.MinSeats, Reservation.MaxSeats);
            Assert.Equal(production.LocationOn(reservation.Date), reservation.Location);
            Assert.True(reservation.AddedAt < FixedClock.Default);
        }
    }

    [Fact]
    public void Demo_reservations_contain_no_duplicates()
    {
        var demo = DemoData.For(Production.RareWereld, FixedClock.Default);

        var keys = demo.Select(reservation => (reservation.FullName, reservation.Date)).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }
}

public class InMemoryReservationStoreTests
{
    [Fact]
    public async Task Gives_seeded_and_added_reservations_distinct_ids()
    {
        var store = new InMemoryReservationStore(
            [Sample.Reservation("A"), Sample.Reservation("B", id: "local-2")]);

        var added = await store.AddAsync(Sample.Reservation("C"));
        var ids = (await store.GetAllAsync()).Select(reservation => reservation.Id).ToList();

        Assert.Equal(3, ids.Distinct().Count());
        Assert.DoesNotContain("", ids);
        Assert.Contains(added.Id, ids);
    }

    [Fact]
    public async Task Updating_something_that_no_longer_exists_fails()
    {
        var store = new InMemoryReservationStore();

        await Assert.ThrowsAsync<StoreException>(() => store.UpdateAsync(Sample.Reservation(id: "gone")));
    }
}
