using Toneel.Core.Firebase;
using Toneel.Core.Reservations;

namespace Toneel.Web.Services;

/// <summary>
/// Sends every call to the store that belongs to the current session:
/// Firestore for an account, browser storage for the demo.
/// </summary>
public sealed class SessionReservationStore(
    AuthState auth,
    FirestoreReservationStore firestore,
    DemoReservationStore demo) : IReservationStore
{
    private IReservationStore Current => auth.Mode switch
    {
        SessionMode.Account => firestore,
        SessionMode.Demo => demo,
        _ => throw new StoreAccessDeniedException("Meld je aan om de reservaties te zien."),
    };

    public Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Guarded(() => Current.GetAllAsync(cancellationToken));

    public Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        Guarded(() => Current.AddAsync(reservation, cancellationToken));

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        Guarded(async () =>
        {
            await Current.UpdateAsync(reservation, cancellationToken);
            return true;
        });

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        Guarded(async () =>
        {
            await Current.DeleteAsync(id, cancellationToken);
            return true;
        });

    /// <summary>
    /// A sign-in problem while talking to the database (an expired session,
    /// no connection to renew the token) is reported like any other store
    /// problem, so pages have one kind of error to handle.
    /// </summary>
    private static async Task<T> Guarded<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (AuthException exception)
        {
            throw exception.Error == AuthError.Network
                ? new StoreException(exception.UserMessage, exception)
                : new StoreAccessDeniedException(exception.UserMessage);
        }
    }
}
