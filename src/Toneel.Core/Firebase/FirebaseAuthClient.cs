using System.Text;
using System.Text.Json;

namespace Toneel.Core.Firebase;

/// <summary>
/// Signs users in with e-mail and password through the Firebase Auth REST API.
/// </summary>
/// <remarks>
/// The REST API is used instead of the JavaScript SDK so that the whole flow
/// is C#, can be unit-tested with a fake <see cref="HttpMessageHandler"/>,
/// and adds nothing to the page's download size.
/// </remarks>
public sealed class FirebaseAuthClient(HttpClient http, FirebaseOptions options, TimeProvider clock)
{
    private const string SignInUrl =
        "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";

    private const string RefreshUrl = "https://securetoken.googleapis.com/v1/token";

    public async Task<AuthSession> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var body = new StringContent(
            JsonSerializer.Serialize(
                new SignInRequest(email.Trim(), password, ReturnSecureToken: true),
                FirebaseJson.Default.SignInRequest),
            Encoding.UTF8,
            "application/json");

        using var json = await PostAsync($"{SignInUrl}?key={Key}", body, cancellationToken);
        var root = json.RootElement;

        return new AuthSession(
            UserId: root.GetProperty("localId").GetString() ?? "",
            Email: root.GetProperty("email").GetString() ?? email.Trim(),
            IdToken: root.GetProperty("idToken").GetString() ?? "",
            RefreshToken: root.GetProperty("refreshToken").GetString() ?? "",
            ExpiresAt: ExpiryFrom(root.GetProperty("expiresIn")));
    }

    /// <summary>Exchanges the refresh token for a new id token.</summary>
    public async Task<AuthSession> RefreshAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        var body = new FormUrlEncodedContent(
        [
            new("grant_type", "refresh_token"),
            new("refresh_token", session.RefreshToken),
        ]);

        using var json = await PostAsync($"{RefreshUrl}?key={Key}", body, cancellationToken);
        var root = json.RootElement;

        return session with
        {
            IdToken = root.GetProperty("id_token").GetString() ?? "",
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? session.RefreshToken,
            ExpiresAt = ExpiryFrom(root.GetProperty("expires_in")),
        };
    }

    private string Key => Uri.EscapeDataString(options.ApiKey);

    private DateTimeOffset ExpiryFrom(JsonElement seconds)
    {
        // Firebase sends the lifetime as a string, e.g. "3600".
        var value = seconds.ValueKind == JsonValueKind.Number
            ? seconds.GetInt32()
            : int.Parse(seconds.GetString() ?? "3600");

        return clock.GetUtcNow().AddSeconds(value);
    }

    private async Task<JsonDocument> PostAsync(
        string url,
        HttpContent body,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        string text;

        try
        {
            response = await http.PostAsync(url, body, cancellationToken);
            text = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AuthException(AuthError.Network, exception.Message, exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var code = ErrorCode(text);
                throw new AuthException(ToAuthError(code), code);
            }
        }

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new AuthException(AuthError.Unknown, "Unreadable response", exception);
        }
    }

    /// <summary>Reads <c>error.message</c> from a Firebase error response.</summary>
    private static string ErrorCode(string responseText)
    {
        try
        {
            using var json = JsonDocument.Parse(responseText);

            return json.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                ? message.GetString() ?? ""
                : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static AuthError ToAuthError(string code)
    {
        // Codes can carry a suffix: "TOO_MANY_ATTEMPTS_TRY_LATER : Access to this account…"
        var name = code.Split(' ', ':')[0];

        return name switch
        {
            "INVALID_LOGIN_CREDENTIALS" or "EMAIL_NOT_FOUND" or "INVALID_PASSWORD"
                or "INVALID_EMAIL" or "MISSING_PASSWORD" => AuthError.InvalidCredentials,
            "USER_DISABLED" => AuthError.UserDisabled,
            "TOO_MANY_ATTEMPTS_TRY_LATER" => AuthError.TooManyAttempts,
            "TOKEN_EXPIRED" or "INVALID_REFRESH_TOKEN" or "USER_NOT_FOUND"
                or "MISSING_REFRESH_TOKEN" => AuthError.SessionExpired,
            _ => AuthError.Unknown,
        };
    }
}
