using System.Net;
using RoomBooking.Domain;
using RoomBooking.Endpoints;
using RoomBooking.Tests.Specs;
using SpecTrace;
using Xunit;

namespace RoomBooking.Tests;

public class RoomsApiTests
{
    [Fact]
    [Verifies(Requirements.RM_001, AcceptanceCriteria.RM_001.AC_1)]
    public async Task Lists_all_rooms_ordered_by_name()
    {
        await using var api = await TestApi.StartAsync();

        var rooms = await api.GetAsync<List<RoomDto>>("/rooms");

        Assert.Equal(new[] { "Atlas", "Boreal", "Cosmos" }, rooms.Select(r => r.Name).ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_001, AcceptanceCriteria.RM_001.AC_2)]
    public async Task Filters_rooms_by_minimum_capacity()
    {
        await using var api = await TestApi.StartAsync();

        var rooms = await api.GetAsync<List<RoomDto>>("/rooms?minCapacity=8");

        Assert.Equal(new[] { "boreal", "cosmos" }, rooms.Select(r => r.Id).ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_002, AcceptanceCriteria.RM_002.AC_1)]
    public async Task Returns_the_details_of_a_room()
    {
        await using var api = await TestApi.StartAsync();

        var room = await api.GetAsync<RoomDto>("/rooms/boreal");

        Assert.Equal(new RoomDto("boreal", "Boreal", 8), room);
    }

    [Fact]
    [Verifies(Requirements.RM_002, AcceptanceCriteria.RM_002.AC_2)]
    public async Task Unknown_room_is_not_found()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.Client.GetAsync("/rooms/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_1)]
    public async Task Room_without_bookings_is_free_all_day()
    {
        await using var api = await TestApi.StartAsync();

        var availability = await api.GetAsync<AvailabilityDto>($"/rooms/atlas/availability?date={TestApi.Day:yyyy-MM-dd}");

        Assert.Equal(new[] { new TimeSlot(new(8, 0), new(20, 0)) }, availability.Slots.ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_2)]
    public async Task Booked_time_is_left_out_of_the_free_slots()
    {
        await using var api = await TestApi.StartAsync();
        await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var availability = await api.GetAsync<AvailabilityDto>($"/rooms/atlas/availability?date={TestApi.Day:yyyy-MM-dd}");

        Assert.Equal(
            new[] { new TimeSlot(new(8, 0), new(10, 0)), new TimeSlot(new(11, 0), new(20, 0)) },
            availability.Slots.ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_3)]
    public async Task Cancelled_bookings_do_not_block_the_room()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        (await api.CancelAsync(booking.Id, "ana@example.com")).EnsureSuccessStatusCode();

        var availability = await api.GetAsync<AvailabilityDto>($"/rooms/atlas/availability?date={TestApi.Day:yyyy-MM-dd}");

        Assert.Equal(new[] { new TimeSlot(new(8, 0), new(20, 0)) }, availability.Slots.ToArray());
    }

    [Fact]
    [Verifies(Requirements.RM_003, AcceptanceCriteria.RM_003.AC_4)]
    public async Task Availability_of_an_unknown_room_is_not_found()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.Client.GetAsync($"/rooms/nope/availability?date={TestApi.Day:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
