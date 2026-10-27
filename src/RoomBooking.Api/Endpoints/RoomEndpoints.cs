using RoomBooking.Domain;
using RoomBooking.Specs;
using SpecTrace;

namespace RoomBooking.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRooms(this IEndpointRouteBuilder app)
    {
        var rooms = app.MapGroup("/rooms").WithTags("Rooms");

        rooms.MapGet("/", ListRooms);
        rooms.MapGet("/{id}", GetRoom);
        rooms.MapGet("/{id}/availability", GetAvailability);

        return app;
    }

    [Implements(Requirements.RM_001)]
    private static IResult ListRooms(BookingService service, int? minCapacity) =>
        Results.Ok(service.ListRooms(minCapacity).Select(RoomDto.From));

    [Implements(Requirements.RM_002)]
    private static IResult GetRoom(BookingService service, string id) =>
        service.FindRoom(id) is { } room
            ? Results.Ok(RoomDto.From(room))
            : Results.Problem($"Room '{id}' does not exist.", statusCode: StatusCodes.Status404NotFound, title: "Not found");

    [Implements(Requirements.RM_003)]
    private static IResult GetAvailability(BookingService service, string id, DateOnly date)
    {
        var result = service.GetAvailability(id, date);
        return result.IsSuccess
            ? Results.Ok(new AvailabilityDto(id, date, result.Value!))
            : result.Error!.ToProblem();
    }
}
