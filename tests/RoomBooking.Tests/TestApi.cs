using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Endpoints;

namespace RoomBooking.Tests;

/// <summary>
/// The real application on Kestrel at a random local port, with its own in-memory data and a fixed clock.
/// Each test starts its own instance, so tests never share bookings.
/// </summary>
public sealed class TestApi : IAsyncDisposable
{
    /// <summary>Monday 7 January 2030, 07:00 office time: before opening on the day the tests book.</summary>
    public static readonly DateTime DefaultNow = new(2030, 1, 7, 7, 0, 0);

    /// <summary>The day the tests book rooms on.</summary>
    public static readonly DateOnly Day = DateOnly.FromDateTime(DefaultNow);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly WebApplication _app;

    private TestApi(WebApplication app, FixedTimeProvider clock, HttpClient client)
    {
        _app = app;
        Clock = clock;
        Client = client;
    }

    public FixedTimeProvider Clock { get; }

    public HttpClient Client { get; }

    public static async Task<TestApi> StartAsync(DateTime? now = null)
    {
        var clock = new FixedTimeProvider(now ?? DefaultNow);
        var app = RoomBookingApp.Build(
            new[] { "--urls", "http://127.0.0.1:0", "--environment", "Testing", "--Logging:LogLevel:Default", "Warning" },
            services => services.AddSingleton<TimeProvider>(clock));

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new TestApi(app, clock, new HttpClient { BaseAddress = new Uri(address) });
    }

    /// <summary>Time of day on <see cref="Day"/>.</summary>
    public static DateTime At(int hour, int minute = 0) => Day.ToDateTime(new TimeOnly(hour, minute));

    public Task<HttpResponseMessage> BookAsync(string roomId, DateTime start, DateTime end, int attendees = 2, string? user = "ana@example.com", string title = "Meeting")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bookings")
        {
            Content = JsonContent.Create(new CreateBookingDto(roomId, title, start, end, attendees), options: Json),
        };
        if (user != null) request.Headers.Add(BookingEndpoints.UserHeader, user);
        return Client.SendAsync(request);
    }

    public async Task<BookingDto> BookOkAsync(string roomId, DateTime start, DateTime end, int attendees = 2, string user = "ana@example.com")
    {
        var response = await BookAsync(roomId, start, end, attendees, user);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(Json))!;
    }

    public Task<HttpResponseMessage> CancelAsync(Guid bookingId, string user)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/bookings/{bookingId}/cancel");
        request.Headers.Add(BookingEndpoints.UserHeader, user);
        return Client.SendAsync(request);
    }

    public async Task<T> GetAsync<T>(string path, string? user = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (user != null) request.Headers.Add(BookingEndpoints.UserHeader, user);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
