using System.Net;
using Toneel.Core.Firebase;

namespace Toneel.Core.Tests;

public class FirebaseAuthClientTests
{
    private static readonly FirebaseOptions Options = new("test-key", "test-project");

    private readonly FakeHttp _http = new();

    private FirebaseAuthClient Client => new(_http.Client, Options, new FixedClock());

    private const string SignInResponse = """
        {
          "localId": "user-1",
          "email": "lotte@example.com",
          "idToken": "id-token",
          "refreshToken": "refresh-token",
          "expiresIn": "3600"
        }
        """;

    /// <summary>The body Firebase sends with a 400: <c>{"error":{"code":400,"message":"…"}}</c>.</summary>
    private static string ErrorResponse(string code) =>
        "{\"error\":{\"code\":400,\"message\":\"" + code + "\"}}";

    [Fact]
    public async Task Signing_in_posts_the_credentials_and_returns_a_session()
    {
        _http.Respond(SignInResponse);

        var session = await Client.SignInAsync(" lotte@example.com ", "geheim");

        var request = Assert.Single(_http.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=test-key",
            request.Url);
        Assert.Equal(
            """{"email":"lotte@example.com","password":"geheim","returnSecureToken":true}""",
            request.Body);

        Assert.Equal("user-1", session.UserId);
        Assert.Equal("lotte@example.com", session.Email);
        Assert.Equal("id-token", session.IdToken);
        Assert.Equal("refresh-token", session.RefreshToken);
        Assert.Equal(FixedClock.Default.AddHours(1), session.ExpiresAt);
    }

    [Theory]
    [InlineData("INVALID_LOGIN_CREDENTIALS", AuthError.InvalidCredentials)]
    [InlineData("EMAIL_NOT_FOUND", AuthError.InvalidCredentials)]
    [InlineData("INVALID_PASSWORD", AuthError.InvalidCredentials)]
    [InlineData("USER_DISABLED", AuthError.UserDisabled)]
    [InlineData("TOO_MANY_ATTEMPTS_TRY_LATER : Access to this account has been temporarily disabled.", AuthError.TooManyAttempts)]
    [InlineData("SOMETHING_NEW", AuthError.Unknown)]
    public async Task Translates_Firebase_error_codes(string code, AuthError expected)
    {
        _http.Respond(ErrorResponse(code), HttpStatusCode.BadRequest);

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => Client.SignInAsync("lotte@example.com", "fout"));

        Assert.Equal(expected, exception.Error);
        Assert.NotEqual("", exception.UserMessage);
    }

    [Fact]
    public async Task A_wrong_email_and_a_wrong_password_give_the_same_message()
    {
        _http.Respond(ErrorResponse("EMAIL_NOT_FOUND"), HttpStatusCode.BadRequest);
        _http.Respond(ErrorResponse("INVALID_PASSWORD"), HttpStatusCode.BadRequest);

        var unknownEmail = await Assert.ThrowsAsync<AuthException>(() => Client.SignInAsync("a@b.c", "x"));
        var wrongPassword = await Assert.ThrowsAsync<AuthException>(() => Client.SignInAsync("a@b.c", "x"));

        Assert.Equal(unknownEmail.UserMessage, wrongPassword.UserMessage);
    }

    [Fact]
    public async Task Reports_a_network_failure_as_such()
    {
        _http.Fail();

        var exception = await Assert.ThrowsAsync<AuthException>(() => Client.SignInAsync("a@b.c", "x"));

        Assert.Equal(AuthError.Network, exception.Error);
    }

    [Fact]
    public async Task An_error_page_that_is_not_JSON_is_an_unknown_error()
    {
        _http.Respond("<html>Bad gateway</html>", HttpStatusCode.BadGateway);

        var exception = await Assert.ThrowsAsync<AuthException>(() => Client.SignInAsync("a@b.c", "x"));

        Assert.Equal(AuthError.Unknown, exception.Error);
    }

    [Fact]
    public async Task Refreshing_exchanges_the_refresh_token_and_keeps_the_user()
    {
        _http.Respond("""{"id_token":"new-id","refresh_token":"new-refresh","expires_in":"3600","user_id":"user-1"}""");
        var old = new AuthSession("user-1", "lotte@example.com", "old-id", "old-refresh", FixedClock.Default);

        var session = await Client.RefreshAsync(old);

        var request = Assert.Single(_http.Requests);
        Assert.Equal("https://securetoken.googleapis.com/v1/token?key=test-key", request.Url);
        Assert.Equal("grant_type=refresh_token&refresh_token=old-refresh", request.Body);

        Assert.Equal("new-id", session.IdToken);
        Assert.Equal("new-refresh", session.RefreshToken);
        Assert.Equal("lotte@example.com", session.Email);
        Assert.Equal("user-1", session.UserId);
        Assert.Equal(FixedClock.Default.AddHours(1), session.ExpiresAt);
    }

    [Theory]
    [InlineData("TOKEN_EXPIRED")]
    [InlineData("INVALID_REFRESH_TOKEN")]
    [InlineData("USER_NOT_FOUND")]
    public async Task A_rejected_refresh_token_means_the_session_expired(string code)
    {
        _http.Respond(ErrorResponse(code), HttpStatusCode.BadRequest);
        var old = new AuthSession("user-1", "a@b.c", "id", "refresh", FixedClock.Default);

        var exception = await Assert.ThrowsAsync<AuthException>(() => Client.RefreshAsync(old));

        Assert.Equal(AuthError.SessionExpired, exception.Error);
    }

    [Fact]
    public void A_token_is_refreshed_a_minute_before_it_expires()
    {
        var session = new AuthSession("u", "e", "id", "refresh", FixedClock.Default.AddMinutes(10));

        Assert.False(session.NeedsRefresh(FixedClock.Default));
        Assert.False(session.NeedsRefresh(FixedClock.Default.AddMinutes(8.9)));
        Assert.True(session.NeedsRefresh(FixedClock.Default.AddMinutes(9)));
        Assert.True(session.NeedsRefresh(FixedClock.Default.AddMinutes(11)));
    }
}

public class AuthSessionJsonTests
{
    [Fact]
    public void A_session_survives_a_round_trip()
    {
        var session = new AuthSession("user-1", "lotte@example.com", "id", "refresh", FixedClock.Default);

        Assert.Equal(session, AuthSessionJson.Deserialize(AuthSessionJson.Serialize(session)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"userId":"u","email":"e","idToken":"i","refreshToken":"","expiresAt":"2026-01-01T00:00:00+00:00"}""")]
    public void Damaged_or_incomplete_text_gives_no_session(string? json)
    {
        Assert.Null(AuthSessionJson.Deserialize(json));
    }
}
