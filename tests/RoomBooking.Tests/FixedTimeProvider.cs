namespace RoomBooking.Tests;

/// <summary>A clock the tests control. Office local time is UTC so test times read as written.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTime localNow) => _now = new DateTimeOffset(localNow, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetNow(DateTime localNow) => _now = new DateTimeOffset(localNow, TimeSpan.Zero);
}
