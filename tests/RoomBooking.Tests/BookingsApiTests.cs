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

    [Theory]
    [InlineData("atlas")]
    [InlineData("boreal")]
    [InlineData("cosmos")]
    [Verifies(Requirements.BK_003, AcceptanceCriteria.BK_003.AC_3)]
    public async Task Zero_attendees_is_rejected(string roomId)
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync(roomId, TestApi.At(10), TestApi.At(11), attendees: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_003, AcceptanceCriteria.BK_003.AC_4)]
    public async Task A_single_attendee_is_allowed()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), attendees: 1);

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
    [Verifies(Requirements.BK_004, AcceptanceCriteria.BK_004.AC_1)]
    public async Task Bookings_from_opening_or_until_closing_time_are_allowed()
    {
        await using var api = await TestApi.StartAsync();

        var fromOpening = await api.BookAsync("atlas", TestApi.At(8), TestApi.At(9));
        var untilClosing = await api.BookAsync("atlas", TestApi.At(19), TestApi.At(20));

        Assert.Equal(HttpStatusCode.Created, fromOpening.StatusCode);
        Assert.Equal(HttpStatusCode.Created, untilClosing.StatusCode);
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

    [Fact]
    [Verifies(Requirements.BK_004, AcceptanceCriteria.BK_004.AC_3)]
    public async Task A_booking_starting_right_now_is_rejected()
    {
        await using var api = await TestApi.StartAsync(TestApi.At(10));

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

    [Fact]
    [Verifies(Requirements.BK_005, AcceptanceCriteria.BK_005.AC_4)]
    public async Task Cancelling_an_unknown_booking_is_not_found()
    {
        await using var api = await TestApi.StartAsync();
        await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.CancelAsync(Guid.NewGuid(), "ana@example.com");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    // ---------- BK-007: Recurring bookings ----------

    [Fact]
    [Verifies(Requirements.BK_007, AcceptanceCriteria.BK_007.AC_1)]
    public async Task Weekly_booking_of_a_free_room_creates_one_booking_per_week()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.BookWeeklyAsync("atlas", TestApi.At(10), TestApi.At(11), weeks: 4);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<RecurringBookingDto>(TestApi.Json))!;
        Assert.Empty(result.Skipped);
        Assert.Equal(
            Enumerable.Range(0, 4).Select(w => TestApi.At(10).AddDays(7 * w)).ToArray(),
            result.Bookings.Select(b => b.Start).ToArray());
        Assert.All(result.Bookings, b => Assert.Equal("atlas", b.RoomId));
        Assert.Equal(4, (await api.GetAsync<List<BookingDto>>("/bookings/mine", user: "ana@example.com")).Count);
    }

    [Fact]
    [Verifies(Requirements.BK_007, AcceptanceCriteria.BK_007.AC_2)]
    public async Task Weekly_booking_skips_and_reports_a_taken_week()
    {
        await using var api = await TestApi.StartAsync();
        var week2 = TestApi.At(10).AddDays(7);
        await api.BookOkAsync("atlas", week2.AddMinutes(30), week2.AddMinutes(90), user: "luis@example.com");

        var response = await api.BookWeeklyAsync("atlas", TestApi.At(10), TestApi.At(11), weeks: 4);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<RecurringBookingDto>(TestApi.Json))!;
        Assert.Equal(3, result.Bookings.Count);
        Assert.DoesNotContain(result.Bookings, b => b.Start == week2);
        var skipped = Assert.Single(result.Skipped);
        Assert.Equal(2, skipped.Week);
        Assert.Equal(week2, skipped.Start);
    }

    [Fact]
    [Verifies(Requirements.BK_007, AcceptanceCriteria.BK_007.AC_3)]
    public async Task Weekly_booking_for_more_than_twelve_weeks_is_rejected()
    {
        await using var api = await TestApi.StartAsync();

        var tooMany = await api.BookWeeklyAsync("atlas", TestApi.At(10), TestApi.At(11), weeks: 13);
        var maximum = await api.BookWeeklyAsync("boreal", TestApi.At(10), TestApi.At(11), weeks: 12);

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.Created, maximum.StatusCode);
        Assert.Equal(12, (await api.GetAsync<List<BookingDto>>("/bookings/mine", user: "ana@example.com")).Count);
    }

    // ---------- BK-008: Move a booking ----------

    private static async Task AssertStillRunsFrom10To11(TestApi api, Guid id)
    {
        var booking = await api.GetAsync<BookingDto>($"/bookings/{id}");
        Assert.Equal(TestApi.At(10), booking.Start);
        Assert.Equal(TestApi.At(11), booking.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_1)]
    public async Task The_organizer_can_move_an_upcoming_booking_to_a_free_range()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moved = await api.GetAsync<BookingDto>($"/bookings/{booking.Id}");
        Assert.Equal(booking.Id, moved.Id);
        Assert.Equal(BookingStatus.Active, moved.Status);
        Assert.Equal(TestApi.At(12), moved.Start);
        Assert.Equal(TestApi.At(13), moved.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_2)]
    public async Task Moving_onto_another_active_booking_is_a_conflict()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        await api.BookOkAsync("atlas", TestApi.At(12), TestApi.At(13), user: "luis@example.com");

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_3)]
    public async Task Another_user_cannot_move_the_booking()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_4)]
    public async Task A_booking_can_move_to_a_range_overlapping_its_own_time()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(10, 30), TestApi.At(11, 30));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moved = await api.GetAsync<BookingDto>($"/bookings/{booking.Id}");
        Assert.Equal(TestApi.At(10, 30), moved.Start);
        Assert.Equal(TestApi.At(11, 30), moved.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_5)]
    public async Task The_old_range_is_free_after_a_move()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        (await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13))).EnsureSuccessStatusCode();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(7, 9)]
    [InlineData(19, 21)]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_6)]
    public async Task Moving_outside_opening_hours_is_rejected(int fromHour, int toHour)
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(fromHour), TestApi.At(toHour));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_7)]
    public async Task Moving_to_a_range_longer_than_four_hours_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(9), TestApi.At(13, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_8)]
    public async Task Moving_to_a_range_in_the_past_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        api.Clock.SetNow(TestApi.At(9, 45));

        var response = await api.MoveAsync(booking.Id, TestApi.At(9), TestApi.At(9, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_9)]
    public async Task Moving_an_unknown_booking_is_not_found()
    {
        await using var api = await TestApi.StartAsync();

        var response = await api.MoveAsync(Guid.NewGuid(), TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_10)]
    public async Task Moving_without_a_user_is_unauthorized()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13), user: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_11)]
    public async Task Moving_to_another_day_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(10).AddDays(1), TestApi.At(11).AddDays(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_12)]
    public async Task A_booking_that_has_started_cannot_be_moved()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        api.Clock.SetNow(TestApi.At(10, 15));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillRunsFrom10To11(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_13)]
    public async Task A_cancelled_booking_cannot_be_moved()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        (await api.CancelAsync(booking.Id, "ana@example.com")).EnsureSuccessStatusCode();

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_14)]
    public async Task A_booking_can_be_moved_to_a_longer_range()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(14));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moved = await api.GetAsync<BookingDto>($"/bookings/{booking.Id}");
        Assert.Equal(booking.Id, moved.Id);
        Assert.Equal(TestApi.At(12), moved.Start);
        Assert.Equal(TestApi.At(14), moved.End);
    }
}
