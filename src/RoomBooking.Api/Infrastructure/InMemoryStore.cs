using RoomBooking.Domain;

namespace RoomBooking.Infrastructure;

/// <summary>
/// Rooms and bookings kept in memory. Every read and write takes the same lock, so the overlap check
/// and the insert of a booking happen atomically.
/// </summary>
public sealed class InMemoryStore
{
    private readonly List<Booking> _bookings = new();

    public InMemoryStore(IEnumerable<Room>? rooms = null)
    {
        Rooms = (rooms ?? DefaultRooms).ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<Room> DefaultRooms { get; } = new[]
    {
        new Room("atlas", "Atlas", 4),
        new Room("boreal", "Boreal", 8),
        new Room("cosmos", "Cosmos", 16),
    };

    public IReadOnlyDictionary<string, Room> Rooms { get; }

    public object Sync { get; } = new();

    /// <summary>Bookings of all rooms. Callers must hold <see cref="Sync"/>.</summary>
    public IReadOnlyList<Booking> Bookings => _bookings;

    /// <summary>Adds a booking. Callers must hold <see cref="Sync"/>.</summary>
    public void Add(Booking booking) => _bookings.Add(booking);
}
