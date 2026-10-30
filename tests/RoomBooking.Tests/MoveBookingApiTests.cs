using System.Net;
using System.Net.Http.Json;
using RoomBooking.Domain;
using RoomBooking.Endpoints;
using RoomBooking.Tests.Specs;
using SpecTrace;
using Xunit;

namespace RoomBooking.Tests;

// ---------- BK-008: Move a booking ----------
public class MoveBookingApiTests
{
    private static async Task<BookingDto> Moved(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BookingDto>(TestApi.Json))!;
    }

    private static async Task AssertStillTenToEleven(TestApi api, Guid id)
    {
        var current = await api.GetAsync<BookingDto>($"/bookings/{id}");
        Assert.Equal(TestApi.At(10), current.Start);
        Assert.Equal(TestApi.At(11), current.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_1)]
    public async Task The_organizer_can_move_a_booking_to_a_free_range()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var moved = await Moved(await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13)));

        Assert.Equal(booking.Id, moved.Id);
        Assert.Equal(BookingStatus.Active, moved.Status);
        Assert.Equal(TestApi.At(12), moved.Start);
        Assert.Equal(TestApi.At(13), moved.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_2)]
    public async Task Moving_onto_another_booking_is_a_conflict()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        await api.BookOkAsync("atlas", TestApi.At(12), TestApi.At(13), user: "luis@example.com");

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_3)]
    public async Task Another_user_cannot_move_the_booking()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_4)]
    public async Task A_booking_can_move_to_a_range_overlapping_its_own_time()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var moved = await Moved(await api.MoveAsync(booking.Id, TestApi.At(10, 30), TestApi.At(11, 30)));

        Assert.Equal(TestApi.At(10, 30), moved.Start);
        Assert.Equal(TestApi.At(11, 30), moved.End);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_5)]
    public async Task The_old_time_is_free_after_moving()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        (await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(13))).EnsureSuccessStatusCode();

        var response = await api.BookAsync("atlas", TestApi.At(10), TestApi.At(11), user: "luis@example.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_6)]
    public async Task Moving_outside_opening_hours_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var early = await api.MoveAsync(booking.Id, TestApi.At(7), TestApi.At(8));
        var late = await api.MoveAsync(booking.Id, TestApi.At(19), TestApi.At(21));

        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, late.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_7)]
    public async Task Moving_to_a_range_longer_than_four_hours_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(9), TestApi.At(13, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_8)]
    public async Task Moving_to_a_range_in_the_past_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));
        api.Clock.SetNow(TestApi.At(9));

        var response = await api.MoveAsync(booking.Id, TestApi.At(8), TestApi.At(8, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
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
    }

    [Fact]
    [Verifies(Requirements.BK_008, AcceptanceCriteria.BK_008.AC_11)]
    public async Task Moving_to_another_day_is_rejected()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var response = await api.MoveAsync(booking.Id, TestApi.At(10).AddDays(1), TestApi.At(11).AddDays(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertStillTenToEleven(api, booking.Id);
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
        await AssertStillTenToEleven(api, booking.Id);
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
    public async Task A_booking_can_move_to_a_longer_range()
    {
        await using var api = await TestApi.StartAsync();
        var booking = await api.BookOkAsync("atlas", TestApi.At(10), TestApi.At(11));

        var moved = await Moved(await api.MoveAsync(booking.Id, TestApi.At(12), TestApi.At(14)));

        Assert.Equal(TestApi.At(12), moved.Start);
        Assert.Equal(TestApi.At(14), moved.End);
    }
}
