namespace Toneel.Core.Firebase;

/// <summary>
/// Identifies the Firebase project.
/// </summary>
/// <remarks>
/// These values are not secrets: every Firebase web app ships them to the
/// browser. What protects the data is the sign-in and the security rules
/// in <c>firestore.rules</c>.
/// </remarks>
public sealed record FirebaseOptions(string ApiKey, string ProjectId)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);
}
