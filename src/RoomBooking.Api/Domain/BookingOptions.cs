namespace RoomBooking.Domain;

/// <summary>Booking rules that can be changed in configuration (section "Booking").</summary>
public sealed class BookingOptions
{
    public const string Section = "Booking";

    public TimeOnly OpensAt { get; set; } = new(8, 0);
    public TimeOnly ClosesAt { get; set; } = new(20, 0);
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromHours(4);
    public int MaxRecurringWeeks { get; set; } = 12;
}
