using Toneel.Core.Schedule;

namespace Toneel.Core.Reservations;

/// <summary>
/// Invented reservations for the demo mode, so visitors can try the app
/// without an account and without seeing anyone's real name.
/// </summary>
public static class DemoData
{
    public static IReadOnlyList<Reservation> For(Production production, DateTimeOffset now)
    {
        (string First, string Last, int Performance, int Seats)[] people =
        [
            ("Lotte", "Vermeulen", 0, 2),
            ("Jef", "De Smet", 0, 4),
            ("Nora", "Claes", 1, 1),
            ("Wout", "Peeters", 1, 3),
            ("Emma", "Janssens", 2, 2),
            ("Kobe", "Maes", 3, 5),
            ("Fien", "Willems", 3, 2),
            ("Arne", "Goossens", 4, 1),
            ("Marie", "Wouters", 5, 6),
            ("Stan", "Jacobs", 5, 2),
            ("Lien", "Verhelst", 6, 3),
            ("Bram", "De Clercq", 7, 2),
            ("Hanne", "Dubois", 7, 4),
            ("Tuur", "Lambrecht", -1, 2),
        ];

        return
        [
            .. people.Select((person, index) =>
            {
                var performance = person.Performance >= 0
                    && person.Performance < production.Performances.Count
                    ? production.Performances[person.Performance]
                    : null;

                return new Reservation
                {
                    FirstName = person.First,
                    LastName = person.Last,
                    Date = performance?.Date,
                    Location = performance?.Location ?? "",
                    Seats = person.Seats,
                    // Spread over the past weeks so "newest first" has a visible order.
                    AddedAt = now.AddDays(-(people.Length - index) * 2).AddMinutes(index * 37),
                };
            }),
        ];
    }
}
