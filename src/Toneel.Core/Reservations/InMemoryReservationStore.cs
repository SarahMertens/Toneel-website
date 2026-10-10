namespace Toneel.Core.Reservations;

/// <summary>Keeps reservations in memory. Used by the tests and as a base for the demo store.</summary>
public class InMemoryReservationStore : IReservationStore
{
    private readonly List<Reservation> _items = [];
    private int _nextId = 1;

    public InMemoryReservationStore(IEnumerable<Reservation>? seed = null)
    {
        foreach (var reservation in seed ?? [])
        {
            _items.Add(WithId(reservation));
        }
    }

    public virtual Task<IReadOnlyList<Reservation>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Reservation>>([.. _items]);

    public virtual Task<Reservation> AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        var saved = WithId(reservation with { Id = "" });
        _items.Add(saved);
        return Task.FromResult(saved);
    }

    public virtual Task UpdateAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == reservation.Id);
        if (index < 0) throw new StoreException($"Reservatie {reservation.Id} bestaat niet meer.");

        _items[index] = reservation;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(item => item.Id == id);
        return Task.CompletedTask;
    }

    private Reservation WithId(Reservation reservation)
    {
        if (reservation.Id != "") return reservation;

        string id;
        do
        {
            id = $"local-{_nextId++}";
        }
        while (_items.Any(item => item.Id == id));

        return reservation with { Id = id };
    }
}
