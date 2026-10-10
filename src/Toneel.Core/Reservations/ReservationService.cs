using System.ComponentModel.DataAnnotations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Reservations;

/// <summary>
/// The rules for adding, changing and removing reservations.
/// Components call this class and never talk to the store directly.
/// </summary>
public sealed class ReservationService(
    IReservationStore store,
    Production production,
    TimeProvider clock)
{
    public Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        store.GetAllAsync(cancellationToken);

    /// <summary>
    /// Returns the reservation that <paramref name="input"/> would duplicate:
    /// the same person on the same date. Reservations without a date are never
    /// duplicates, because nothing is known yet about when the person comes.
    /// </summary>
    /// <param name="ignoreId">The reservation being edited, which may match itself.</param>
    public static Reservation? FindDuplicate(
        IEnumerable<Reservation> existing,
        ReservationInput input,
        string? ignoreId = null)
    {
        if (input.Date is null) return null;

        return existing.FirstOrDefault(reservation =>
            reservation.Id != ignoreId
            && reservation.IsSameBookingAs(input.FirstName, input.LastName, input.Date));
    }

    public Task<Reservation> AddAsync(
        ReservationInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);

        return store.AddAsync(
            new Reservation
            {
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Date = input.Date,
                Location = production.LocationOn(input.Date),
                Seats = input.Seats,
                AddedAt = clock.GetUtcNow(),
            },
            cancellationToken);
    }

    public async Task<Reservation> UpdateAsync(
        Reservation original,
        ReservationInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);

        var updated = original with
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Date = input.Date,
            Location = production.LocationOn(input.Date),
            Seats = input.Seats,
        };

        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>
    /// Adds <paramref name="extraSeats"/> to an existing reservation instead
    /// of creating a second one for the same person and date.
    /// </summary>
    /// <param name="replaces">
    /// When a reservation was edited into a duplicate of <paramref name="target"/>,
    /// pass it here: it is removed after its seats have been added to the target.
    /// </param>
    public async Task<Reservation> MergeAsync(
        Reservation target,
        int extraSeats,
        Reservation? replaces = null,
        CancellationToken cancellationToken = default)
    {
        var merged = target with
        {
            Seats = Math.Min(target.Seats + extraSeats, Reservation.MaxSeats),
        };

        await store.UpdateAsync(merged, cancellationToken);

        if (replaces is not null)
        {
            await store.DeleteAsync(replaces.Id, cancellationToken);
        }

        return merged;
    }

    public Task DeleteAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        store.DeleteAsync(reservation.Id, cancellationToken);

    /// <summary>
    /// The form already validates, but the service is the last stop before
    /// the database, so it does not rely on every caller having done so.
    /// </summary>
    private void Validate(ReservationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName))
        {
            throw new ValidationException("Vul een voornaam en een achternaam in.");
        }

        if (input.FirstName.Trim().Length > ReservationInput.MaxNameLength
            || input.LastName.Trim().Length > ReservationInput.MaxNameLength)
        {
            throw new ValidationException("Een naam mag hoogstens 60 tekens lang zijn.");
        }

        if (input.Seats is < Reservation.MinSeats or > Reservation.MaxSeats)
        {
            throw new ValidationException("Kies een aantal tussen 1 en 100.");
        }

        if (input.Date is not null && production.Find(input.Date) is null)
        {
            throw new ValidationException("Op deze datum is er geen voorstelling.");
        }
    }
}
