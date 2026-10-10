namespace Toneel.Core.Reservations;

/// <summary>A booking by one person for one performance, for one or more seats.</summary>
public sealed record Reservation
{
    public const int MinSeats = 1;
    public const int MaxSeats = 100;

    /// <summary>Assigned by the store; empty until the reservation is saved.</summary>
    public string Id { get; init; } = "";

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    /// <summary><c>null</c> while the visitor has not chosen a performance yet.</summary>
    public DateOnly? Date { get; init; }

    public string Location { get; init; } = "";

    public int Seats { get; init; } = MinSeats;

    public DateTimeOffset AddedAt { get; init; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Same person on the same date, ignoring upper and lower case. Two such
    /// reservations are one booking that should be merged.
    /// </summary>
    public bool IsSameBookingAs(string firstName, string lastName, DateOnly? date) =>
        Date == date
        && string.Equals(FirstName, firstName.Trim(), StringComparison.CurrentCultureIgnoreCase)
        && string.Equals(LastName, lastName.Trim(), StringComparison.CurrentCultureIgnoreCase);
}
