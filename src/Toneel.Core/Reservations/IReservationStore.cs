namespace Toneel.Core.Reservations;

/// <summary>
/// Where reservations are kept. The rest of the app does not know whether
/// that is Firestore, the browser or memory.
/// </summary>
public interface IReservationStore
{
    Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves a new reservation and returns it with its id.</summary>
    Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>The store could not be reached or refused the request.</summary>
public class StoreException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The signed-in user is not allowed to read or change reservations.</summary>
public sealed class StoreAccessDeniedException(string message) : StoreException(message);
