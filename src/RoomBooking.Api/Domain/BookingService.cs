using Microsoft.Extensions.Options;
using RoomBooking.Infrastructure;
using RoomBooking.Specs;
using SpecTrace;

namespace RoomBooking.Domain;

public sealed record BookingRequest(string RoomId, string Title, DateTime Start, DateTime End, int Attendees);

/// <summary>The booking rules. HTTP concerns live in the endpoints.</summary>
public sealed class BookingService
{
    private readonly InMemoryStore _store;
    private readonly TimeProvider _time;
    private readonly BookingOptions _options;

    public BookingService(InMemoryStore store, TimeProvider time, IOptions<BookingOptions> options)
    {
        _store = store;
        _time = time;
        _options = options.Value;
    }

    /// <summary>Current office local time.</summary>
    private DateTime Now => _time.GetLocalNow().DateTime;

    [Implements(Requirements.RM_001)]
    public IReadOnlyList<Room> ListRooms(int? minCapacity = null) =>
        _store.Rooms.Values
            .Where(r => minCapacity is null || r.Capacity >= minCapacity)
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    [Implements(Requirements.RM_002)]
    public Room? FindRoom(string id) => _store.Rooms.GetValueOrDefault(id);

    public Result<IReadOnlyList<TimeSlot>> GetAvailability(string roomId, DateOnly date)
    {
        if (FindRoom(roomId) is not { } room) return Error.NotFound($"Room '{roomId}' does not exist.");

        lock (_store.Sync)
        {
            var bookings = _store.Bookings.Where(b => b.RoomId == room.Id).ToList();
            return Result<IReadOnlyList<TimeSlot>>.Ok(Availability.FreeSlots(date, _options.OpensAt, _options.ClosesAt, bookings));
        }
    }

    [Implements(Requirements.BK_001)]
    public Result<Booking> Create(BookingRequest request, string organizer)
    {
        if (FindRoom(request.RoomId) is not { } room) return Error.NotFound($"Room '{request.RoomId}' does not exist.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Error.Invalid("The title is required.");
        if (request.End <= request.Start) return Error.Invalid("The end time must be after the start time.");
        if (CheckSchedule(request) is { } scheduleError) return scheduleError;
        if (CheckCapacity(room, request.Attendees) is { } capacityError) return capacityError;

        lock (_store.Sync)
        {
            if (FindConflict(room, request.Start, request.End) is { } conflict)
            {
                return Error.Conflict($"{room.Name} is already booked from {conflict.Start:HH:mm} to {conflict.End:HH:mm}.");
            }

            var booking = new Booking(Guid.NewGuid(), room.Id, organizer, request.Title.Trim(), request.Start, request.End, request.Attendees);
            _store.Add(booking);
            return booking;
        }
    }

    public Booking? FindBooking(Guid id)
    {
        lock (_store.Sync)
        {
            return _store.Bookings.FirstOrDefault(b => b.Id == id);
        }
    }

    [Implements(Requirements.BK_005)]
    public Result<Booking> Cancel(Guid bookingId, string user)
    {
        lock (_store.Sync)
        {
            var booking = _store.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking is null) return Error.NotFound($"Booking '{bookingId}' does not exist.");
            if (!string.Equals(booking.Organizer, user, StringComparison.OrdinalIgnoreCase))
            {
                return Error.Forbidden("Only the organizer can cancel a booking.");
            }

            if (!booking.IsActive) return Error.Conflict("The booking is already cancelled.");
            if (booking.Start <= Now) return Error.Conflict("The booking has already started and can no longer be cancelled.");

            booking.Cancel();
            return booking;
        }
    }

    [Implements(Requirements.BK_006)]
    public IReadOnlyList<Booking> ListUpcoming(string organizer)
    {
        var now = Now;
        lock (_store.Sync)
        {
            return _store.Bookings
                .Where(b => b.IsActive && b.End > now)
                .Where(b => string.Equals(b.Organizer, organizer, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.Start)
                .ToList();
        }
    }

    [Implements(Requirements.BK_004)]
    private Error? CheckSchedule(BookingRequest request)
    {
        if (request.Start <= Now) return Error.Invalid("The booking must start in the future.");

        var opens = DateOnly.FromDateTime(request.Start).ToDateTime(_options.OpensAt);
        var closes = DateOnly.FromDateTime(request.Start).ToDateTime(_options.ClosesAt);
        if (request.Start < opens || request.End > closes)
        {
            return Error.Invalid($"Bookings must be between {_options.OpensAt:HH:mm} and {_options.ClosesAt:HH:mm} on the same day.");
        }

        if (request.End - request.Start > _options.MaxDuration)
        {
            return Error.Invalid($"A booking can last at most {_options.MaxDuration.TotalHours:0.#} hours.");
        }

        return null;
    }

    [Implements(Requirements.BK_003)]
    private static Error? CheckCapacity(Room room, int attendees)
    {
        if (attendees < 1) return Error.Invalid("A booking needs at least one attendee.");
        return attendees > room.Capacity
            ? Error.Invalid($"{room.Name} holds {room.Capacity} people; {attendees} were requested.")
            : null;
    }

    /// <summary>Caller must hold the store lock.</summary>
    [Implements(Requirements.BK_002)]
    private Booking? FindConflict(Room room, DateTime start, DateTime end) =>
        _store.Bookings.FirstOrDefault(b => b.RoomId == room.Id && b.IsActive && b.Overlaps(start, end));
}
