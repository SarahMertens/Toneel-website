using Toneel.Core.Firebase;

namespace Toneel.Web.Services;

public enum SessionMode
{
    SignedOut,

    /// <summary>Signed in with a Firebase account; data lives in Firestore.</summary>
    Account,

    /// <summary>Trying the app with invented data that stays in this browser.</summary>
    Demo,
}

/// <summary>
/// Who is using the app right now. Components read it to decide what to
/// show and subscribe to <see cref="Changed"/> to re-render.
/// </summary>
public sealed class AuthState(
    FirebaseAuthClient auth,
    BrowserStorage storage,
    FirebaseOptions options,
    TimeProvider clock)
{
    private const string SessionKey = "toneel.session";
    private const string DemoKey = "toneel.demo";

    private AuthSession? _session;

    public event Action? Changed;

    /// <summary>False until the stored session has been read; the app shows nothing before that.</summary>
    public bool IsInitialized { get; private set; }

    public SessionMode Mode { get; private set; }

    public string Email => _session?.Email ?? "";

    /// <summary>False when no Firebase project is filled in; then only the demo is available.</summary>
    public bool CanSignIn => options.IsConfigured;

    /// <summary>Picks up where the previous visit left off.</summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        _session = AuthSessionJson.Deserialize(await storage.GetAsync(SessionKey));

        Mode = _session is not null ? SessionMode.Account
            : await storage.GetAsync(DemoKey) is not null ? SessionMode.Demo
            : SessionMode.SignedOut;

        IsInitialized = true;
        Changed?.Invoke();
    }

    /// <exception cref="AuthException">Wrong credentials, no connection, …</exception>
    public async Task SignInAsync(string email, string password)
    {
        _session = await auth.SignInAsync(email, password);
        await storage.SetAsync(SessionKey, AuthSessionJson.Serialize(_session));
        await storage.RemoveAsync(DemoKey);

        Mode = SessionMode.Account;
        Changed?.Invoke();
    }

    public async Task StartDemoAsync()
    {
        await storage.SetAsync(DemoKey, "1");

        Mode = SessionMode.Demo;
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        _session = null;
        await storage.RemoveAsync(SessionKey);
        await storage.RemoveAsync(DemoKey);

        Mode = SessionMode.SignedOut;
        Changed?.Invoke();
    }

    /// <summary>
    /// A token that is valid right now. Id tokens live for an hour; when the
    /// current one is about to expire it is renewed first, so the user is
    /// not asked for a password again.
    /// </summary>
    /// <exception cref="AuthException">The session can no longer be renewed.</exception>
    public async Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_session is null) throw new AuthException(AuthError.SessionExpired);

        if (_session.NeedsRefresh(clock.GetUtcNow()))
        {
            try
            {
                _session = await auth.RefreshAsync(_session, cancellationToken);
                await storage.SetAsync(SessionKey, AuthSessionJson.Serialize(_session));
            }
            catch (AuthException exception) when (
                exception.Error is AuthError.SessionExpired or AuthError.UserDisabled)
            {
                await SignOutAsync();
                throw;
            }
        }

        return _session.IdToken;
    }
}
