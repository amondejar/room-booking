using RoomBooking.Specs;
using SpecTrace;

namespace RoomBooking.Domain;

public static class Availability
{
    /// <summary>
    /// Free slots of one room on <paramref name="date"/> between opening and closing time,
    /// leaving out the time taken by active bookings.
    /// </summary>
    [Implements(Requirements.RM_003)]
    public static IReadOnlyList<TimeSlot> FreeSlots(DateOnly date, TimeOnly opensAt, TimeOnly closesAt, IEnumerable<Booking> bookings)
    {
        var dayStart = date.ToDateTime(opensAt);
        var dayEnd = date.ToDateTime(closesAt);

        var taken = bookings
            .Where(b => b.IsActive && b.Overlaps(dayStart, dayEnd))
            .OrderBy(b => b.Start);

        var slots = new List<TimeSlot>();
        var cursor = dayStart;
        foreach (var booking in taken)
        {
            if (booking.Start > cursor) slots.Add(Slot(cursor, booking.Start));
            if (booking.End > cursor) cursor = booking.End;
        }

        if (cursor < dayEnd) slots.Add(Slot(cursor, dayEnd));
        return slots;
    }

    private static TimeSlot Slot(DateTime start, DateTime end) => new(TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end));
}
