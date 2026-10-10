using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toneel.Core.Firebase;

/// <summary>Reads and writes an <see cref="AuthSession"/> as JSON, to remember it between visits.</summary>
public static class AuthSessionJson
{
    public static string Serialize(AuthSession session) =>
        JsonSerializer.Serialize(session, AuthSessionJsonContext.Default.AuthSession);

    /// <summary>Returns <c>null</c> when the text is missing, damaged or incomplete.</summary>
    public static AuthSession? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var session = JsonSerializer.Deserialize(json, AuthSessionJsonContext.Default.AuthSession);

            return session is { RefreshToken.Length: > 0, IdToken: not null, Email: not null, UserId: not null }
                ? session
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AuthSession))]
internal sealed partial class AuthSessionJsonContext : JsonSerializerContext;
