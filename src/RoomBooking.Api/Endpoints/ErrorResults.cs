using RoomBooking.Domain;

namespace RoomBooking.Endpoints;

internal static class ErrorResults
{
    /// <summary>Maps a business rule error to an RFC 9457 problem response.</summary>
    public static IResult ToProblem(this Error error) => error.Kind switch
    {
        ErrorKind.Invalid => Results.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest, title: "The request breaks a booking rule"),
        ErrorKind.NotFound => Results.Problem(error.Message, statusCode: StatusCodes.Status404NotFound, title: "Not found"),
        ErrorKind.Conflict => Results.Problem(error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ErrorKind.Forbidden => Results.Problem(error.Message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden"),
        _ => Results.Problem(error.Message),
    };
}
