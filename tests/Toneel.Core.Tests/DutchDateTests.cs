using Toneel.Core.Schedule;

namespace Toneel.Core.Tests;

public class DutchDateTests
{
    [Theory]
    [InlineData(2026, 1, 24, "24 januari 2026")]
    [InlineData(2026, 2, 1, "1 februari 2026")]
    [InlineData(2026, 10, 7, "7 oktober 2026")]
    [InlineData(2028, 2, 29, "29 februari 2028")]
    public void Formats_a_date_the_way_the_database_stores_it(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, DutchDate.Format(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData("24 januari 2026", 2026, 1, 24)]
    [InlineData("1 februari 2026", 2026, 2, 1)]
    [InlineData("3 Oktober 2026", 2026, 10, 3)]
    [InlineData("  7   oktober   2026 ", 2026, 10, 7)]
    public void Parses_what_it_formats(string text, int year, int month, int day)
    {
        Assert.True(DutchDate.TryParse(text, out var date));
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nog niet gekozen")]
    [InlineData("24 january 2026")]
    [InlineData("31 februari 2026")]
    [InlineData("0 januari 2026")]
    [InlineData("24 januari")]
    [InlineData("24 januari 0")]
    [InlineData("x januari 2026")]
    public void Rejects_text_that_is_not_a_date(string? text)
    {
        Assert.False(DutchDate.TryParse(text, out _));
    }

    [Fact]
    public void Every_day_of_a_year_survives_a_round_trip()
    {
        for (var date = new DateOnly(2026, 1, 1); date.Year == 2026; date = date.AddDays(1))
        {
            Assert.True(DutchDate.TryParse(DutchDate.Format(date), out var parsed));
            Assert.Equal(date, parsed);
        }
    }

    [Fact]
    public void Names_the_weekday()
    {
        var saturday = new DateOnly(2026, 1, 24);

        Assert.Equal("zaterdag 24 januari", DutchDate.FormatWithWeekday(saturday));
        Assert.Equal("za", DutchDate.ShortWeekday(saturday));
        Assert.Equal("jan", DutchDate.ShortMonth(saturday));
        Assert.Equal("zo", DutchDate.ShortWeekday(saturday.AddDays(1)));
    }
}
