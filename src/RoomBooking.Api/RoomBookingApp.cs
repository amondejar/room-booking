using System.Text.Json.Serialization;
using RoomBooking.Domain;
using RoomBooking.Endpoints;
using RoomBooking.Infrastructure;

namespace RoomBooking;

/// <summary>Builds the web application. Tests call it with their own clock and a random port.</summary>
public static class RoomBookingApp
{
    public static WebApplication Build(string[] args, Action<IServiceCollection>? overrides = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProblemDetails();
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.Section));
        builder.Services.AddSingleton(TimeProvider.System);
        // Factory: the container would otherwise pass an empty IEnumerable<Room> instead of the default rooms.
        builder.Services.AddSingleton(_ => new InMemoryStore());
        builder.Services.AddSingleton<BookingService>();
        overrides?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapGet("/", () => Results.Redirect("/rooms")).ExcludeFromDescription();
        app.MapRooms();
        app.MapBookings();

        return app;
    }
}
