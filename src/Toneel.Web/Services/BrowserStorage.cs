using Microsoft.JSInterop;

namespace Toneel.Web.Services;

/// <summary>
/// The browser's <c>localStorage</c>. Storage can be unavailable (private
/// mode, blocked site data), so reads fall back to "nothing stored" and
/// writes report whether they worked instead of throwing.
/// </summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task<bool> SetAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (JSException)
        {
            // Nothing to remove if storage is unavailable.
        }
    }
}
