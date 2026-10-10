using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toneel.Core.Reservations;

/// <summary>Reads and writes a list of reservations as JSON, for storage in the browser.</summary>
public static class ReservationJson
{
    public static string Serialize(IEnumerable<Reservation> reservations) =>
        JsonSerializer.Serialize([.. reservations], ReservationJsonContext.Default.ListReservation);

    /// <summary>Returns <c>null</c> when the text is missing or not a valid list.</summary>
    public static List<Reservation>? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize(json, ReservationJsonContext.Default.ListReservation);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<Reservation>))]
internal sealed partial class ReservationJsonContext : JsonSerializerContext;
