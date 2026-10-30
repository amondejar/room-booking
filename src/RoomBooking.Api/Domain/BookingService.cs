using Microsoft.Extensions.Options;
using RoomBooking.Infrastructure;
using RoomBooking.Specs;
using SpecTrace;

namespace RoomBooking.Domain;

public sealed record BookingRequest(string RoomId, string Title, DateTime Start, DateTime End, int Attendees);

/// <summary>A week of a recurring booking that could not be booked. Weeks are numbered from 1.</summary>
public sealed record SkippedWeek(int Week, DateTime Start, DateTime End, string Reason);

public sealed record RecurringBookingResult(IReadOnlyList<Booking> Created, IReadOnlyList<SkippedWeek> Skipped);

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
        var validated = Validate(request);
        if (validated.Error is { } error) return error;
        var room = validated.Value!;

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

    /// <summary>
    /// Books the same room and time once a week. The rules that are the same every week (room, title,
    /// opening hours, duration, capacity) are checked on the first occurrence and reject the whole request;
    /// weeks whose time is already taken are skipped and reported.
    /// </summary>
    [Implements(Requirements.BK_007)]
    public Result<RecurringBookingResult> CreateWeekly(BookingRequest request, int weeks, string organizer)
    {
        if (weeks < 1 || weeks > _options.MaxRecurringWeeks)
        {
            return Error.Invalid($"A recurring booking must cover between 1 and {_options.MaxRecurringWeeks} weeks.");
        }

        var validated = Validate(request);
        if (validated.Error is { } error) return error;
        var room = validated.Value!;

        lock (_store.Sync)
        {
            var created = new List<Booking>();
            var skipped = new List<SkippedWeek>();
            for (var week = 1; week <= weeks; week++)
            {
                var offset = TimeSpan.FromDays(7 * (week - 1));
                var start = request.Start + offset;
                var end = request.End + offset;

                if (FindConflict(room, start, end) is { } conflict)
                {
                    skipped.Add(new SkippedWeek(week, start, end,
                        $"{room.Name} is already booked from {conflict.Start:HH:mm} to {conflict.End:HH:mm}."));
                    continue;
                }

                var booking = new Booking(Guid.NewGuid(), room.Id, organizer, request.Title.Trim(), start, end, request.Attendees);
                _store.Add(booking);
                created.Add(booking);
            }

            return new RecurringBookingResult(created, skipped);
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

    /// <summary>Moves an upcoming active booking to another time range on the same day; otherwise leaves it unchanged.</summary>
    [Implements(Requirements.BK_008)]
    public Result<Booking> Move(Guid bookingId, DateTime start, DateTime end, string user)
    {
        lock (_store.Sync)
        {
            var booking = _store.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking is null) return Error.NotFound($"Booking '{bookingId}' does not exist.");
            if (!string.Equals(booking.Organizer, user, StringComparison.OrdinalIgnoreCase))
            {
                return Error.Forbidden("Only the organizer can move a booking.");
            }

            if (!booking.IsActive) return Error.Conflict("A cancelled booking cannot be moved.");
            if (booking.Start <= Now) return Error.Conflict("The booking has already started and can no longer be moved.");

            if (end <= start) return Error.Invalid("The end time must be after the start time.");
            if (start.Date != booking.Start.Date || end.Date != start.Date)
            {
                return Error.Invalid("A booking can only be moved to another time on the same day.");
            }

            if (CheckSchedule(new BookingRequest(booking.RoomId, booking.Title, start, end, booking.Attendees)) is { } scheduleError)
            {
                return scheduleError;
            }

            var conflict = _store.Bookings.FirstOrDefault(b =>
                b.Id != booking.Id && b.RoomId == booking.RoomId && b.IsActive && b.Overlaps(start, end));
            if (conflict is not null)
            {
                return Error.Conflict($"The room is already booked from {conflict.Start:HH:mm} to {conflict.End:HH:mm}.");
            }

            booking.Move(start, end);
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

    /// <summary>The rules a booking must meet before checking for overlaps.</summary>
    private Result<Room> Validate(BookingRequest request)
    {
        if (FindRoom(request.RoomId) is not { } room) return Error.NotFound($"Room '{request.RoomId}' does not exist.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Error.Invalid("The title is required.");
        if (request.End <= request.Start) return Error.Invalid("The end time must be after the start time.");
        if (CheckSchedule(request) is { } scheduleError) return scheduleError;
        if (CheckCapacity(room, request.Attendees) is { } capacityError) return capacityError;
        return room;
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
