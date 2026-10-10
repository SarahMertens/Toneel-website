using System.Globalization;
using System.Text;
using System.Text.Json;
using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Core.Firebase;

/// <summary>
/// Converts between a <see cref="Reservation"/> and a Firestore document.
/// </summary>
/// <remarks>
/// The field names and formats are those of the first version of the site
/// (<c>firstName</c>, <c>date</c> as "24 januari 2026", <c>amount</c>,
/// <c>addedAt</c> in milliseconds), so existing data keeps working.
/// The REST API wraps every value in its type:
/// <c>{ "amount": { "integerValue": "2" } }</c>.
/// </remarks>
public static class FirestoreDocument
{
    public static string ToJson(Reservation reservation)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteStartObject("fields");

            WriteString(json, "firstName", reservation.FirstName);
            WriteString(json, "lastName", reservation.LastName);
            WriteString(json, "date", reservation.Date is { } date ? DutchDate.Format(date) : "");
            WriteString(json, "location", reservation.Location);
            WriteInteger(json, "amount", reservation.Seats);
            WriteInteger(json, "addedAt", reservation.AddedAt.ToUnixTimeMilliseconds());

            json.WriteEndObject();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <param name="document">A document as returned by the REST API.</param>
    public static Reservation FromJson(JsonElement document)
    {
        // "projects/<project>/databases/(default)/documents/people/<id>"
        var path = document.GetProperty("name").GetString() ?? "";
        var id = path[(path.LastIndexOf('/') + 1)..];

        var fields = document.TryGetProperty("fields", out var value) ? value : default;

        var hasDate = DutchDate.TryParse(ReadString(fields, "date"), out var date);
        var seats = (int)Math.Clamp(
            ReadNumber(fields, "amount") ?? Reservation.MinSeats,
            Reservation.MinSeats,
            Reservation.MaxSeats);

        return new Reservation
        {
            Id = id,
            FirstName = ReadString(fields, "firstName"),
            LastName = ReadString(fields, "lastName"),
            Date = hasDate ? date : null,
            Location = ReadString(fields, "location"),
            Seats = seats,
            AddedAt = DateTimeOffset.FromUnixTimeMilliseconds(ReadNumber(fields, "addedAt") ?? 0),
        };
    }

    private static void WriteString(Utf8JsonWriter json, string name, string value)
    {
        json.WriteStartObject(name);
        json.WriteString("stringValue", value);
        json.WriteEndObject();
    }

    private static void WriteInteger(Utf8JsonWriter json, string name, long value)
    {
        json.WriteStartObject(name);
        // 64-bit integers travel as strings, because JSON numbers lose precision.
        json.WriteString("integerValue", value.ToString(CultureInfo.InvariantCulture));
        json.WriteEndObject();
    }

    private static string ReadString(JsonElement fields, string name) =>
        TryGetValue(fields, name, "stringValue", out var value) ? value.GetString() ?? "" : "";

    /// <summary>Accepts integers, doubles and numeric strings: old data contains all three.</summary>
    private static long? ReadNumber(JsonElement fields, string name)
    {
        if (TryGetValue(fields, name, "integerValue", out var integer))
        {
            return integer.ValueKind == JsonValueKind.Number
                ? integer.GetInt64()
                : ParseLong(integer.GetString());
        }

        if (TryGetValue(fields, name, "doubleValue", out var number)
            && number.ValueKind == JsonValueKind.Number
            && double.IsFinite(number.GetDouble()))
        {
            return (long)number.GetDouble();
        }

        return TryGetValue(fields, name, "stringValue", out var text)
            ? ParseLong(text.GetString())
            : null;
    }

    private static long? ParseLong(string? text) =>
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static bool TryGetValue(
        JsonElement fields,
        string name,
        string type,
        out JsonElement value)
    {
        value = default;

        return fields.ValueKind == JsonValueKind.Object
            && fields.TryGetProperty(name, out var field)
            && field.ValueKind == JsonValueKind.Object
            && field.TryGetProperty(type, out value);
    }
}
