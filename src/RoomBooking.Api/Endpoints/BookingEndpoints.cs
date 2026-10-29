using RoomBooking.Domain;
using RoomBooking.Specs;
using SpecTrace;

namespace RoomBooking.Endpoints;

public static class BookingEndpoints
{
    /// <summary>
    /// Demo identity: the organizer comes from this header. Replace it with real authentication
    /// (for example JWT bearer and the user's email claim) before going to production.
    /// </summary>
    public const string UserHeader = "X-User";

    public static IEndpointRouteBuilder MapBookings(this IEndpointRouteBuilder app)
    {
        var bookings = app.MapGroup("/bookings").WithTags("Bookings");

        bookings.MapPost("/", CreateBooking);
        bookings.MapPost("/recurring", CreateRecurringBooking);
        bookings.MapGet("/mine", ListMine);
        bookings.MapGet("/{id:guid}", GetBooking).WithName(nameof(GetBooking));
        bookings.MapPost("/{id:guid}/cancel", CancelBooking);
        bookings.MapPost("/{id:guid}/move", MoveBooking);

        return app;
    }

    [Implements(Requirements.BK_001)]
    private static IResult CreateBooking(BookingService service, HttpRequest http, CreateBookingDto body)
    {
        if (User(http) is not { } user) return Unauthorized();

        var result = service.Create(new BookingRequest(body.RoomId, body.Title, body.Start, body.End, body.Attendees), user);
        return result.IsSuccess
            ? Results.CreatedAtRoute(nameof(GetBooking), new { id = result.Value!.Id }, BookingDto.From(result.Value))
            : result.Error!.ToProblem();
    }

    [Implements(Requirements.BK_007)]
    private static IResult CreateRecurringBooking(BookingService service, HttpRequest http, CreateRecurringBookingDto body)
    {
        if (User(http) is not { } user) return Unauthorized();

        var result = service.CreateWeekly(new BookingRequest(body.RoomId, body.Title, body.Start, body.End, body.Attendees), body.Weeks, user);
        return result.IsSuccess
            ? Results.Json(RecurringBookingDto.From(result.Value!), statusCode: StatusCodes.Status201Created)
            : result.Error!.ToProblem();
    }

    private static IResult GetBooking(BookingService service, Guid id) =>
        service.FindBooking(id) is { } booking
            ? Results.Ok(BookingDto.From(booking))
            : Results.Problem($"Booking '{id}' does not exist.", statusCode: StatusCodes.Status404NotFound, title: "Not found");

    [Implements(Requirements.BK_005)]
    private static IResult CancelBooking(BookingService service, HttpRequest http, Guid id)
    {
        if (User(http) is not { } user) return Unauthorized();

        var result = service.Cancel(id, user);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }

    [Implements(Requirements.BK_008)]
    private static IResult MoveBooking(BookingService service, HttpRequest http, Guid id, MoveBookingDto body)
    {
        if (User(http) is not { } user) return Unauthorized();

        var result = service.Move(id, body.Start, body.End, user);
        return result.IsSuccess ? Results.Ok(BookingDto.From(result.Value!)) : result.Error!.ToProblem();
    }

    [Implements(Requirements.BK_006)]
    private static IResult ListMine(BookingService service, HttpRequest http) =>
        User(http) is { } user
            ? Results.Ok(service.ListUpcoming(user).Select(BookingDto.From))
            : Unauthorized();

    private static string? User(HttpRequest http) =>
        http.Headers.TryGetValue(UserHeader, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString().Trim() : null;

    private static IResult Unauthorized() =>
        Results.Problem($"Send the organizer in the {UserHeader} header.", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
}
