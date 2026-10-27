namespace RoomBooking.Domain;

/// <summary>A free range of time within a day.</summary>
public sealed record TimeSlot(TimeOnly Start, TimeOnly End);
