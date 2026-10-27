using RoomBooking.Domain;
using RoomBooking.Tests.Specs;
using SpecTrace;
using Xunit;

namespace RoomBooking.Tests;

/// <summary>The free-slot calculation on its own, for cases that are awkward to set up through the API.</summary>
public class AvailabilityTests
{
    private static readonly DateOnly Day = new(2030, 1, 7);

    private static Booking At(int fromHour, int toHour) =>
        new(Guid.NewGuid(), "atlas", "ana@example.com", "x", Day.ToDateTime(new TimeOnly(fromHour, 0)), Day.ToDateTime(new TimeOnly(toHour, 0)), 2);

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_2)]
    public void Back_to_back_and_overlapping_bookings_merge_into_one_busy_block()
    {
        var slots = Availability.FreeSlots(Day, new(8, 0), new(20, 0), new[] { At(13, 15), At(9, 10), At(10, 12), At(11, 12) });

        Assert.Equal(
            new[] { new TimeSlot(new(8, 0), new(9, 0)), new TimeSlot(new(12, 0), new(13, 0)), new TimeSlot(new(15, 0), new(20, 0)) },
            slots.ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_2)]
    public void A_fully_booked_day_has_no_free_slots()
    {
        var slots = Availability.FreeSlots(Day, new(8, 0), new(20, 0), new[] { At(8, 12), At(12, 16), At(16, 20) });

        Assert.Empty(slots);
    }
}
