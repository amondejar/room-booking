namespace RoomBooking.Domain;

/// <summary>A meeting room that can be booked.</summary>
public sealed record Room(string Id, string Name, int Capacity);
