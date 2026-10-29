namespace RoomBooking.Domain;

public enum BookingStatus
{
    Active,
    Cancelled,
}

/// <summary>A room booked by an organizer for a time range. Times are office local time.</summary>
public sealed class Booking
{
    public Booking(Guid id, string roomId, string organizer, string title, DateTime start, DateTime end, int attendees)
    {
        Id = id;
        RoomId = roomId;
        Organizer = organizer;
        Title = title;
        Start = start;
        End = end;
        Attendees = attendees;
    }

    public Guid Id { get; }
    public string RoomId { get; }
    public string Organizer { get; }
    public string Title { get; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public int Attendees { get; }
    public BookingStatus Status { get; private set; } = BookingStatus.Active;

    public bool IsActive => Status == BookingStatus.Active;

    /// <summary>Half-open ranges: a booking that ends at 11:00 does not overlap one that starts at 11:00.</summary>
    public bool Overlaps(DateTime start, DateTime end) => Start < end && start < End;

    internal void Move(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    internal void Cancel() => Status = BookingStatus.Cancelled;
}
