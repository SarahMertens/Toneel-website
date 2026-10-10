using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Toneel.Core.Reservations;

namespace Toneel.Core.Firebase;

/// <summary>
/// Keeps reservations in the Firestore collection <c>people</c>, through the
/// REST API. Every request carries the id token of the signed-in user; the
/// rules in <c>firestore.rules</c> decide whether it is allowed.
/// </summary>
/// <param name="getIdToken">Returns a valid id token, refreshing it when needed.</param>
public sealed class FirestoreReservationStore(
    HttpClient http,
    FirebaseOptions options,
    Func<CancellationToken, Task<string>> getIdToken) : IReservationStore
{
    public const string Collection = "people";

    private const int PageSize = 300;

    private string CollectionUrl =>
        "https://firestore.googleapis.com/v1/projects/" +
        $"{Uri.EscapeDataString(options.ProjectId)}/databases/(default)/documents/{Collection}";

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var reservations = new List<Reservation>();
        string? pageToken = null;

        // The API returns the collection in pages; keep going until there is no next page.
        do
        {
            var url = $"{CollectionUrl}?pageSize={PageSize}";
            if (pageToken is not null) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using var json = await SendAsync(HttpMethod.Get, url, body: null, cancellationToken);

            if (json.RootElement.TryGetProperty("documents", out var documents))
            {
                reservations.AddRange(documents.EnumerateArray().Select(FirestoreDocument.FromJson));
            }

            pageToken = json.RootElement.TryGetProperty("nextPageToken", out var next)
                ? next.GetString()
                : null;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return reservations;
    }

    public async Task<Reservation> AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        using var json = await SendAsync(
            HttpMethod.Post,
            CollectionUrl,
            FirestoreDocument.ToJson(reservation),
            cancellationToken);

        return FirestoreDocument.FromJson(json.RootElement);
    }

    public async Task UpdateAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        // currentDocument.exists=true: fail instead of silently re-creating a
        // reservation that someone else deleted in the meantime.
        using var _ = await SendAsync(
            HttpMethod.Patch,
            $"{DocumentUrl(reservation.Id)}?currentDocument.exists=true",
            FirestoreDocument.ToJson(reservation),
            cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Delete, DocumentUrl(id), body: null, cancellationToken);
    }

    private string DocumentUrl(string id) => $"{CollectionUrl}/{Uri.EscapeDataString(id)}";

    private async Task<JsonDocument> SendAsync(
        HttpMethod method,
        string url,
        string? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await getIdToken(cancellationToken));

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        HttpStatusCode status;
        string text;

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            status = response.StatusCode;
            text = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new StoreException(
                "Geen verbinding met de database. Controleer je internet.",
                exception);
        }

        switch (status)
        {
            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                throw new StoreAccessDeniedException(
                    "Dit account heeft geen toegang tot de reservaties.");

            case HttpStatusCode.NotFound or HttpStatusCode.PreconditionFailed or HttpStatusCode.BadRequest
                when method == HttpMethod.Patch:
                throw new StoreException(
                    "Deze reservatie bestaat niet meer. Iemand anders heeft ze mogelijk verwijderd.");

            case >= HttpStatusCode.BadRequest:
                throw new StoreException($"De database gaf een fout ({(int)status}).");
        }

        try
        {
            // A successful DELETE answers with an empty object.
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        }
        catch (JsonException exception)
        {
            throw new StoreException("De database gaf een onleesbaar antwoord.", exception);
        }
    }
}
