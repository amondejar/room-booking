using RoomBooking.Domain;

namespace RoomBooking.Endpoints;

public sealed record RoomDto(string Id, string Name, int Capacity)
{
    public static RoomDto From(Room room) => new(room.Id, room.Name, room.Capacity);
}

public sealed record AvailabilityDto(string RoomId, DateOnly Date, IReadOnlyList<TimeSlot> Slots);

public sealed record CreateBookingDto(string RoomId, string Title, DateTime Start, DateTime End, int Attendees);

public sealed record BookingDto(Guid Id, string RoomId, string Title, string Organizer, DateTime Start, DateTime End, int Attendees, BookingStatus Status)
{
    public static BookingDto From(Booking b) => new(b.Id, b.RoomId, b.Title, b.Organizer, b.Start, b.End, b.Attendees, b.Status);
}
