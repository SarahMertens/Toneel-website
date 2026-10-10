using Toneel.Core.Reservations;

namespace Toneel.Core.Tests;

public class ReservationQueryTests
{
    private static readonly Reservation[] All =
    [
        Sample.Reservation("Lotte", "Vermeulen", Sample.Saturday, id: "lotte", addedMinutesAgo: 30),
        Sample.Reservation("Jef", "De Smet", Sample.Sunday, id: "jef", addedMinutesAgo: 10),
        Sample.Reservation("Nora", "Claes", undated: true, id: "nora", addedMinutesAgo: 20),
    ];

    /// <summary>The ids of the matching reservations, in order: "jef,nora".</summary>
    private static string Ids(ReservationFilter filter) =>
        string.Join(',', ReservationQuery.Apply(All, filter).Select(reservation => reservation.Id));

    [Fact]
    public void Without_a_filter_everything_is_shown_newest_first()
    {
        Assert.Equal("jef,nora,lotte", Ids(default));
        Assert.True(default(ReservationFilter).IsEmpty);
    }

    [Theory]
    [InlineData("lotte", "lotte")]
    [InlineData("SMET", "jef")]
    [InlineData("zaal zuid", "jef")]
    [InlineData("24 januari", "lotte")]
    [InlineData("nog niet", "nora")]
    [InlineData("  claes  ", "nora")]
    public void Searches_name_date_and_location(string search, string expectedId)
    {
        Assert.Equal(expectedId, Ids(new ReservationFilter(search)));
    }

    [Fact]
    public void Every_word_of_the_search_must_match()
    {
        Assert.Equal("jef", Ids(new ReservationFilter("jef zuid")));
        Assert.Equal("", Ids(new ReservationFilter("jef noord")));
    }

    [Fact]
    public void Filters_on_one_performance()
    {
        Assert.Equal("jef", Ids(new ReservationFilter(Date: DateFilter.On(Sample.Sunday))));
    }

    [Fact]
    public void Filters_on_reservations_without_a_date()
    {
        Assert.Equal("nora", Ids(new ReservationFilter(Date: DateFilter.Undated)));
    }

    [Fact]
    public void Combines_search_and_date_filter()
    {
        Assert.Equal("", Ids(new ReservationFilter("lotte", DateFilter.On(Sample.Sunday))));
        Assert.Equal("lotte", Ids(new ReservationFilter("lotte", DateFilter.On(Sample.Saturday))));
    }
}
