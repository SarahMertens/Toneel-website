namespace Toneel.Core.Schedule;

/// <summary>
/// A play with its ticket price and performances. Everything the app shows
/// about "which play" comes from one instance of this class.
/// </summary>
public sealed class Production
{
    private readonly Dictionary<DateOnly, Performance> _byDate;

    public Production(
        string company,
        string title,
        decimal ticketPrice,
        IEnumerable<Performance> performances)
    {
        Company = company;
        Title = title;
        TicketPrice = ticketPrice;
        Performances = [.. performances.OrderBy(performance => performance.Date)];
        _byDate = Performances.ToDictionary(performance => performance.Date);
    }

    public string Company { get; }

    public string Title { get; }

    /// <summary>Price of one seat, in euro.</summary>
    public decimal TicketPrice { get; }

    /// <summary>In chronological order.</summary>
    public IReadOnlyList<Performance> Performances { get; }

    public Performance? Find(DateOnly? date) =>
        date is { } value && _byDate.TryGetValue(value, out var performance)
            ? performance
            : null;

    /// <summary>The location follows from the date; empty when no date is chosen.</summary>
    public string LocationOn(DateOnly? date) => Find(date)?.Location ?? "";

    /// <summary>"Er was eens … Rare Wereld!" by Toneelgroep De Ballon, 2026.</summary>
    public static Production RareWereld { get; } = new(
        company: "Toneelgroep De Ballon",
        title: "Er was eens … Rare Wereld!",
        ticketPrice: 8m,
        performances:
        [
            new(new DateOnly(2026, 1, 24), "OC Het Kasteeltje"),
            new(new DateOnly(2026, 1, 31), "OC De Boeie"),
            new(new DateOnly(2026, 2, 1), "OC De Boeie"),
            new(new DateOnly(2026, 2, 7), "OC De Schelpe"),
            new(new DateOnly(2026, 2, 8), "OC De Schelpe"),
            new(new DateOnly(2026, 2, 14), "OC De Blomme"),
            new(new DateOnly(2026, 2, 15), "OC De Blomme"),
            new(new DateOnly(2026, 2, 22), "OC De Ballon"),
            new(new DateOnly(2026, 10, 3), "OC 't Viooltje"),
            new(new DateOnly(2026, 10, 7), "Dienstencentrum Biezenbilk"),
        ]);
}
