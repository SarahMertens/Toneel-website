using System.Net;
using System.Text.Json;
using Toneel.Core.Firebase;
using Toneel.Core.Reservations;

namespace Toneel.Core.Tests;

public class FirestoreDocumentTests
{
    private static Reservation Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return FirestoreDocument.FromJson(document.RootElement);
    }

    [Fact]
    public void Reads_a_document_written_by_the_first_version_of_the_site()
    {
        var reservation = Parse("""
            {
              "name": "projects/p/databases/(default)/documents/people/AbC123",
              "fields": {
                "firstName": { "stringValue": "Lotte" },
                "lastName": { "stringValue": "Vermeulen" },
                "date": { "stringValue": "24 januari 2026" },
                "location": { "stringValue": "OC Het Kasteeltje" },
                "amount": { "integerValue": "3" },
                "addedAt": { "integerValue": "1767268800000" }
              }
            }
            """);

        Assert.Equal("AbC123", reservation.Id);
        Assert.Equal("Lotte", reservation.FirstName);
        Assert.Equal("Vermeulen", reservation.LastName);
        Assert.Equal(new DateOnly(2026, 1, 24), reservation.Date);
        Assert.Equal("OC Het Kasteeltje", reservation.Location);
        Assert.Equal(3, reservation.Seats);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), reservation.AddedAt);
    }

    [Theory]
    [InlineData("""{ "doubleValue": 4 }""", 4)]
    [InlineData("""{ "doubleValue": 4.0 }""", 4)]
    [InlineData("""{ "stringValue": "5" }""", 5)]
    [InlineData("""{ "integerValue": 6 }""", 6)]
    [InlineData("""{ "integerValue": "0" }""", 1)]
    [InlineData("""{ "integerValue": "5000" }""", 100)]
    [InlineData("""{ "stringValue": "veel" }""", 1)]
    [InlineData("""{ "nullValue": null }""", 1)]
    public void Reads_the_amount_whatever_type_it_was_stored_as(string value, int expected)
    {
        var reservation = Parse($$"""
            { "name": "x/people/1", "fields": { "amount": {{value}} } }
            """);

        Assert.Equal(expected, reservation.Seats);
    }

    [Fact]
    public void Missing_fields_become_empty_values_instead_of_errors()
    {
        var reservation = Parse("""{ "name": "x/people/1" }""");

        Assert.Equal("1", reservation.Id);
        Assert.Equal("", reservation.FirstName);
        Assert.Null(reservation.Date);
        Assert.Equal(1, reservation.Seats);
        Assert.Equal(DateTimeOffset.UnixEpoch, reservation.AddedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nog niet gekozen")]
    public void A_date_that_is_not_a_date_means_not_chosen_yet(string stored)
    {
        var reservation = Parse($$"""
            { "name": "x/people/1", "fields": { "date": { "stringValue": "{{stored}}" } } }
            """);

        Assert.Null(reservation.Date);
    }

    [Fact]
    public void Writes_the_same_field_names_and_types_as_the_first_version()
    {
        var json = FirestoreDocument.ToJson(Sample.Reservation("Lotte", "Vermeulen", seats: 3, id: "ignored"));

        Assert.Equal(
            """{"fields":{"firstName":{"stringValue":"Lotte"},"lastName":{"stringValue":"Vermeulen"},"date":{"stringValue":"24 januari 2026"},"location":{"stringValue":"Zaal Noord"},"amount":{"integerValue":"3"},"addedAt":{"integerValue":"1768046400000"}}}""",
            json);
    }

    [Fact]
    public void What_is_written_can_be_read_back()
    {
        var original = Sample.Reservation("Zoë", "D'Hondt \"de 2e\"", seats: 7, id: "abc", addedMinutesAgo: 3);

        var fields = FirestoreDocument.ToJson(original);
        var roundTripped = Parse($$"""{ "name": "x/people/abc", {{fields[1..]}}""");

        Assert.Equal(original, roundTripped);
    }
}

public class FirestoreReservationStoreTests
{
    private const string Base =
        "https://firestore.googleapis.com/v1/projects/test-project/databases/(default)/documents/people";

    private readonly FakeHttp _http = new();

    private FirestoreReservationStore Store => new(
        _http.Client,
        new FirebaseOptions("test-key", "test-project"),
        _ => Task.FromResult("the-id-token"));

    private static string Document(string id, string firstName) => $$"""
        { "name": "projects/test-project/databases/(default)/documents/people/{{id}}",
          "fields": { "firstName": { "stringValue": "{{firstName}}" } } }
        """;

    [Fact]
    public async Task Every_request_carries_the_id_token_of_the_signed_in_user()
    {
        _http.Respond("{}");

        await Store.GetAllAsync();

        Assert.Equal("Bearer the-id-token", Assert.Single(_http.Requests).Authorization);
    }

    [Fact]
    public async Task An_empty_collection_is_an_empty_list()
    {
        _http.Respond("{}");

        Assert.Empty(await Store.GetAllAsync());
    }

    [Fact]
    public async Task Follows_the_next_page_token_until_everything_is_read()
    {
        _http.Respond($$"""{ "documents": [{{Document("1", "Lotte")}}], "nextPageToken": "page 2" }""");
        _http.Respond($$"""{ "documents": [{{Document("2", "Jef")}}] }""");

        var all = await Store.GetAllAsync();

        Assert.Equal("Lotte,Jef", string.Join(',', all.Select(reservation => reservation.FirstName)));
        Assert.Equal(2, _http.Requests.Count);
        Assert.Equal($"{Base}?pageSize=300", _http.Requests[0].Url);
        Assert.Equal($"{Base}?pageSize=300&pageToken=page%202", _http.Requests[1].Url);
    }

    [Fact]
    public async Task Adding_posts_the_fields_and_returns_the_reservation_with_its_new_id()
    {
        _http.Respond(Document("new-id", "Lotte"));

        var saved = await Store.AddAsync(Sample.Reservation("Lotte"));

        var request = Assert.Single(_http.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Base, request.Url);
        Assert.StartsWith("""{"fields":{"firstName":{"stringValue":"Lotte"}""", request.Body);
        Assert.Equal("new-id", saved.Id);
    }

    [Fact]
    public async Task Updating_patches_the_document_and_requires_that_it_still_exists()
    {
        _http.Respond(Document("abc", "Lotte"));

        await Store.UpdateAsync(Sample.Reservation(id: "abc"));

        var request = Assert.Single(_http.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"{Base}/abc?currentDocument.exists=true", request.Url);
    }

    [Fact]
    public async Task Updating_a_reservation_that_was_deleted_elsewhere_explains_what_happened()
    {
        _http.Respond("""{"error":{"status":"NOT_FOUND"}}""", HttpStatusCode.NotFound);

        var exception = await Assert.ThrowsAsync<StoreException>(
            () => Store.UpdateAsync(Sample.Reservation(id: "gone")));

        Assert.Contains("bestaat niet meer", exception.Message);
    }

    [Fact]
    public async Task Deleting_sends_a_delete_for_that_document()
    {
        _http.Respond("{}");

        await Store.DeleteAsync("abc");

        var request = Assert.Single(_http.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"{Base}/abc", request.Url);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task A_refusal_by_the_security_rules_is_reported_as_access_denied(HttpStatusCode status)
    {
        _http.Respond("""{"error":{"status":"PERMISSION_DENIED"}}""", status);

        await Assert.ThrowsAsync<StoreAccessDeniedException>(() => Store.GetAllAsync());
    }

    [Fact]
    public async Task A_server_error_is_a_store_exception()
    {
        _http.Respond("oops", HttpStatusCode.InternalServerError);

        var exception = await Assert.ThrowsAsync<StoreException>(() => Store.GetAllAsync());

        Assert.Contains("500", exception.Message);
    }

    [Fact]
    public async Task Being_offline_is_a_store_exception_with_a_readable_message()
    {
        _http.Fail();

        var exception = await Assert.ThrowsAsync<StoreException>(() => Store.GetAllAsync());

        Assert.Contains("verbinding", exception.Message);
    }
}
