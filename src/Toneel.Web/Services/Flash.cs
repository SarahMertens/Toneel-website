namespace Toneel.Web.Services;

/// <summary>
/// A message for the next page: the form sets "Reservatie toegevoegd" and
/// the overview shows it once after the redirect.
/// </summary>
public sealed class Flash
{
    private string? _message;

    public void Set(string message) => _message = message;

    /// <summary>Returns the message and forgets it.</summary>
    public string? Take()
    {
        var message = _message;
        _message = null;
        return message;
    }
}
