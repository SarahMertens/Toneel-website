using System.Text.Json.Serialization;

namespace Toneel.Core.Firebase;

internal sealed record SignInRequest(string Email, string Password, bool ReturnSecureToken);

/// <summary>
/// Source-generated JSON metadata. Reflection-based serialisation would be
/// removed by the trimmer when the app is published for the browser.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SignInRequest))]
internal sealed partial class FirebaseJson : JsonSerializerContext;
