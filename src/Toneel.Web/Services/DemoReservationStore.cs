using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Web.Services;

/// <summary>
/// The store behind the demo: invented reservations that are kept in this
/// browser's storage and never leave the device.
/// </summary>
public sealed class DemoReservationStore(
    BrowserStorage storage,
    Production production,
    TimeProvider clock) : IReservationStore
{
    private const string Key = "toneel.demo.reservations";

    private InMemoryReservationStore? _items;

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await (await ItemsAsync()).GetAllAsync(cancellationToken);

    public async Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        var saved = await (await ItemsAsync()).AddAsync(reservation, cancellationToken);
        await SaveAsync();
        return saved;
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await (await ItemsAsync()).UpdateAsync(reservation, cancellationToken);
        await SaveAsync();
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await (await ItemsAsync()).DeleteAsync(id, cancellationToken);
        await SaveAsync();
    }

    /// <summary>Throws away all changes and starts again from the example data.</summary>
    public async Task ResetAsync()
    {
        await storage.RemoveAsync(Key);
        _items = null;
    }

    private async Task<InMemoryReservationStore> ItemsAsync()
    {
        if (_items is not null) return _items;

        var stored = ReservationJson.Deserialize(await storage.GetAsync(Key));
        _items = new InMemoryReservationStore(stored ?? DemoData.For(production, clock.GetUtcNow()));

        if (stored is null) await SaveAsync();

        return _items;
    }

    private async Task SaveAsync() =>
        await storage.SetAsync(Key, ReservationJson.Serialize(await _items!.GetAllAsync()));
}
