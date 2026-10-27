namespace RoomBooking.Domain;

public enum ErrorKind
{
    Invalid,
    NotFound,
    Conflict,
    Forbidden,
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error Invalid(string message) => new(ErrorKind.Invalid, message);
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
}

/// <summary>The outcome of an operation: a value or the business rule that stopped it.</summary>
public readonly struct Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(Error error) => new(default, error);
}
