namespace Toneel.Core.Firebase;

/// <summary>A signed-in user and the tokens that prove it.</summary>
/// <param name="IdToken">Short-lived token sent with every database request.</param>
/// <param name="RefreshToken">Long-lived token used to get a new <paramref name="IdToken"/>.</param>
public sealed record AuthSession(
    string UserId,
    string Email,
    string IdToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt)
{
    /// <summary>True when the id token is expired or about to expire.</summary>
    public bool NeedsRefresh(DateTimeOffset now) => now >= ExpiresAt - TimeSpan.FromMinutes(1);
}

public enum AuthError
{
    InvalidCredentials,
    UserDisabled,
    TooManyAttempts,
    SessionExpired,
    Network,
    Unknown,
}

public sealed class AuthException(AuthError error, string? detail = null, Exception? inner = null)
    : Exception(detail ?? error.ToString(), inner)
{
    public AuthError Error { get; } = error;

    /// <summary>What to tell the user.</summary>
    public string UserMessage => Error switch
    {
        // Deliberately the same text for a wrong e-mail and a wrong password,
        // so the form does not reveal which accounts exist.
        AuthError.InvalidCredentials => "Het e-mailadres of wachtwoord klopt niet.",
        AuthError.UserDisabled => "Dit account is uitgeschakeld.",
        AuthError.TooManyAttempts => "Te veel pogingen. Probeer het over enkele minuten opnieuw.",
        AuthError.SessionExpired => "Je sessie is verlopen. Meld je opnieuw aan.",
        AuthError.Network => "Geen verbinding met de aanmelddienst. Controleer je internet.",
        _ => "Aanmelden is niet gelukt. Probeer het opnieuw.",
    };
}
