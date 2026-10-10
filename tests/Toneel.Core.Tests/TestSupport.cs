using System.Net;
using System.Text;
using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Tests;

/// <summary>A clock that always shows the same moment.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    public FixedClock()
        : this(Default)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
/// Stands in for the network: records every request and answers with the
/// queued responses, in order.
/// </summary>
public sealed class FakeHttp : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<(HttpMethod Method, string Url, string? Authorization, string Body)> Requests { get; } = [];

    public HttpClient Client => new(this);

    public FakeHttp Respond(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue(() => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return this;
    }

    public FakeHttp Fail()
    {
        _responses.Enqueue(() => throw new HttpRequestException("offline"));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add((
            request.Method,
            request.RequestUri!.AbsoluteUri,
            request.Headers.Authorization?.ToString(),
            request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));

        return _responses.Dequeue()();
    }
}

public static class Sample
{
    public static readonly DateOnly Saturday = new(2026, 1, 24);
    public static readonly DateOnly Sunday = new(2026, 1, 25);

    /// <summary>A small production: two performances, 8 euro a seat.</summary>
    public static Production Production { get; } = new(
        "Toneelgroep Test",
        "Proefstuk",
        ticketPrice: 8m,
        [new(Sunday, "Zaal Zuid"), new(Saturday, "Zaal Noord")]);

    public static Reservation Reservation(
        string firstName = "Lotte",
        string lastName = "Vermeulen",
        DateOnly? date = null,
        int seats = 1,
        string id = "",
        int addedMinutesAgo = 0,
        bool undated = false)
    {
        DateOnly? actualDate = undated ? null : date ?? Saturday;

        return new Reservation
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Date = actualDate,
            Location = Production.LocationOn(actualDate),
            Seats = seats,
            AddedAt = FixedClock.Default.AddMinutes(-addedMinutesAgo),
        };
    }

    public static ReservationInput Input(
        string firstName = "Lotte",
        string lastName = "Vermeulen",
        DateOnly? date = null,
        int seats = 1,
        bool undated = false) => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Date = undated ? null : date ?? Saturday,
            Seats = seats,
        };
}
