using System.Net;
using System.Net.Http.Json;
using RoomBooking.Domain;
using RoomBooking.Endpoints;
using RoomBooking.Tests.Specs;
using SpecTrace;
using Xunit;

namespace RoomBooking.Tests;

public class BookingsApiTests
{
    // ---------- BK-001: Create a booking ----------

    [Fact]
    [Verifies(Requirements.BK_001, AcceptanceCriteria.BK_001.AC_1)]
    public async Task Booking_a_free_room_creates_an_active_booking()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), attendees: 3);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = (await response.Content.ReadFromJsonAsync<BookingDto>(TestApi.Json))!;
        Assert.Equal(BookingStatus.Active, booking.Status);
        Assert.Equal("ana@example.com", booking.Organizer);
        Assert.Equal($"/bookings/{booking.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(booking.Id, (await api.GetAsync<BookingDto>(response.Headers.Location.AbsolutePath)).Id);
    }

    [Theory]
    [InlineData(11, 10)]
    [InlineData(10, 10)]
    [Verifies(Requirements.BK_001, AcceptanceCriteria.BK_001.AC_2)]
    public async Task End_not_after_start_is_rejected(int fromHour, int toHour)
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(fromHour), TestApi.At(toHour));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_001, AcceptanceCriteria.BK_001.AC_3)]
    public async Task Booking_an_unknown_room_is_not_found()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("nope", TestApi.At(10), TestApi.At(11));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_001, AcceptanceCriteria.BK_001.AC_4)]
    public async Task Booking_without_a_user_is_unauthorized()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), user: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- BK-002: Prevent double booking ----------

    [Fact]
    [Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_1)]
    public async Task Overlapping_booking_of_the_same_room_is_a_conflict()
    {
        await using var api = await TestApi.StartAsync();
        await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.BookAsync("atlas", TestApi.At(10, 30), TestApi.At(11, 30), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_2)]
    public async Task Back_to_back_bookings_are_allowed()
    {
        await using var api = await TestApi.StartAsync();
        await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.BookAsync("atlas", TestApi.At(11), TestApi.At(12));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_3)]
    public async Task A_cancelled_booking_frees_its_time()
    {
        await using var api = await TestApi.StartAsync();
        var first = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        (await api.CancelAsync(first.Id, "ana@example.com")).EnsureSuccessStatusCode();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_4)]
    public async Task The_same_time_can_be_booked_in_another_room()
    {
        await using var api = await TestApi.StartAsync();
        await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.BookAsync("boreal", TestApi.At(10), TestApi.At(11));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_002, AcceptanceCriteria.BK_002.AC_1)]
    public async Task Concurrent_requests_for_the_same_slot_create_only_one_booking()
    {
        await using var api = await TestApi.StartAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            api.BookAsync("cosmos", TestApi.At(15), TestApi.At(16), user: $"user{i}@example.com")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    // ---------- BK-003: Respect room capacity ----------

    [Fact]
    [Verifies(Requirements.BK_003, AcceptanceCriteria.BK_003.AC_1)]
    public async Task More_attendees_than_capacity_is_rejected()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), attendees: 5);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_003, AcceptanceCriteria.BK_003.AC_2)]
    public async Task Exactly_the_capacity_is_allowed()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), attendees: 4);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---------- BK-004: Opening hours and duration ----------

    [Theory]
    [InlineData(7, 0, 9, 0)]
    [InlineData(19, 0, 20, 30)]
    [Verifies(Requirements.BK_004, AcceptanceCriteria.BK_004.AC_1)]
    public async Task Bookings_outside_opening_hours_are_rejected(int fromHour, int fromMinute, int toHour, int toMinute)
    {
        await using var api = await TestApi.StartAsync(TestApi.Day.AddDays(-1).ToDateTime(new TimeOnly(12, 0)));

        var response = await api.BookAsync("atlas", TestApi.At(fromHour, fromMinute), TestApi.At(toHour, toMinute));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_004, AcceptanceCriteria.BK_004.AC_2)]
    public async Task Bookings_longer_than_four_hours_are_rejected()
    {
        await using var api = await TestApi.StartAsync();

        var tooLong = await api.BookAsync("atlas", TestApi.At(9), TestApi.At(13, 30));
        var maximum = await api.BookAsync("atlas", TestApi.At(14), TestApi.At(18));

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal(HttpStatusCode.Created, maximum.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_004, AcceptanceCriteria.BK_004.AC_3)]
    public async Task Bookings_in_the_past_are_rejected()
    {
        await using var api = await TestApi.StartAsync(TestApi.At(12));

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- BK-005: Cancel a booking ----------

    [Fact]
    [Verifies(Requirements.BK_005, AcceptanceCriteria.BK_005.AC_1)]
    public async Task The_organizer_can_cancel_an_upcoming_booking()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.CancelAsync(booking.Id, "ana@example.com");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(BookingStatus.Cancelled, (await api.GetAsync<BookingDto>($"/bookings/{booking.Id}")).Status);
    }

    [Fact]
    [Verifies(Requirements.BK_005, AcceptanceCriteria.BK_005.AC_2)]
    public async Task Another_user_cannot_cancel_the_booking()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.CancelAsync(booking.Id, "luis@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BookingStatus.Active, (await api.GetAsync<BookingDto>($"/bookings/{booking.Id}")).Status);
    }

    [Fact]
    [Verifies(Requirements.BK_005, AcceptanceCriteria.BK_005.AC_3)]
    public async Task A_booking_that_has_started_cannot_be_cancelled()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        api.Clock.SetNow(TestApi.At(10, 15));

        var response = await api.CancelAsync(booking.Id, "ana@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- BK-006: My upcoming bookings ----------

    [Fact]
    [Verifies(Requirements.BK_006, AcceptanceCriteria.BK_006.AC_1)]
    public async Task Organizers_see_only_their_bookings_ordered_by_start()
    {
        await using var api = await TestApi.StartAsync();
        var late = await api.BookOkAsync("atlas", TestApi.At(15), TestApi.At(16), user: "ana@example.com");
        await api.BookOkAsync("boreal", TestApi.At(9), TestApi.At(10), user: "luis@example.com");
        var early = await api.BookOkAsync("cosmos", TestApi.At(9), TestApi.At(10), user: "ana@example.com");

        var mine = await api.GetAsync<List<BookingDto>>("/bookings/mine", user: "ana@example.com");

        Assert.Equal(new[] { early.Id, late.Id }, mine.Select(b => b.Id).ToArray());
    }

    [Fact]
    [Verifies(Requirements.BK_006, AcceptanceCriteria.BK_006.AC_2)]
    public async Task Cancelled_and_finished_bookings_are_not_listed()
    {
        await using var api = await TestApi.StartAsync();
        var finished = await api.BookOkAsync("atlas", TestApi.At(8), TestApi.At(9));
        var cancelled = await api.BookOkAsync("atlas", TestApi.At(12), TestApi.At(13));
        var upcoming = await api.BookOkAsync("atlas", TestApi.At(17), TestApi.At(18));
        (await api.CancelAsync(cancelled.Id, "ana@example.com")).EnsureSuccessStatusCode();
        api.Clock.SetNow(TestApi.At(9, 30));

        var mine = await api.GetAsync<List<BookingDto>>("/bookings/mine", user: "ana@example.com");

        Assert.Equal(new[] { upcoming.Id }, mine.Select(b => b.Id).ToArray());
        Assert.DoesNotContain(mine, b => b.Id == finished.Id || b.Id == cancelled.Id);
    }
}
