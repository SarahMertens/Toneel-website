using Toneel.Core.Schedule;

namespace Toneel.Core.Tests;

public class ProductionTests
{
    [Fact]
    public void Lists_performances_in_chronological_order()
    {
        var dates = Sample.Production.Performances.Select(performance => performance.Date).ToList();

        Assert.Equal(2, dates.Count);
        Assert.Equal(Sample.Saturday, dates[0]);
        Assert.Equal(Sample.Sunday, dates[1]);
    }

    [Fact]
    public void The_location_follows_from_the_date()
    {
        Assert.Equal("Zaal Noord", Sample.Production.LocationOn(Sample.Saturday));
        Assert.Equal("", Sample.Production.LocationOn(null));
        Assert.Equal("", Sample.Production.LocationOn(new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void Rare_Wereld_keeps_the_dates_and_locations_of_the_original_site()
    {
        var expected = new Dictionary<string, string>
        {
            ["24 januari 2026"] = "OC Het Kasteeltje",
            ["31 januari 2026"] = "OC De Boeie",
            ["1 februari 2026"] = "OC De Boeie",
            ["7 februari 2026"] = "OC De Schelpe",
            ["8 februari 2026"] = "OC De Schelpe",
            ["14 februari 2026"] = "OC De Blomme",
            ["15 februari 2026"] = "OC De Blomme",
            ["22 februari 2026"] = "OC De Ballon",
            ["3 oktober 2026"] = "OC 't Viooltje",
            ["7 oktober 2026"] = "Dienstencentrum Biezenbilk",
        };

        var actual = Production.RareWereld.Performances.ToDictionary(
            performance => DutchDate.Format(performance.Date),
            performance => performance.Location);

        Assert.Equal(expected.Count, actual.Count);
        foreach (var (date, location) in expected)
        {
            Assert.Equal(location, actual[date]);
        }

        Assert.Equal(8m, Production.RareWereld.TicketPrice);
    }
}
