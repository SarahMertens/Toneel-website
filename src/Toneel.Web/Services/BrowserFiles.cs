using Microsoft.JSInterop;
using Toneel.Core.Export;
using Toneel.Core.Schedule;

namespace Toneel.Web.Services;

/// <summary>
/// The two things that need JavaScript: saving a file and drawing on a
/// canvas. Everything else in the app is C#.
/// </summary>
public sealed class BrowserFiles(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>Path of the ticket background, relative to the site root.</summary>
    public const string TicketImage = "img/ticket-rare-wereld.jpg";

    private IJSObjectReference? _module;

    public async Task DownloadAsync(ExportFile file)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("downloadFile", file.FileName, file.ContentType, file.Content);
    }

    /// <summary>Prints the ticket's text on the background image and saves it as a PNG.</summary>
    /// <exception cref="JSException">The image could not be loaded.</exception>
    public async Task DownloadTicketAsync(Ticket ticket)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync(
            "downloadTicket",
            TicketImage,
            ticket.FileName,
            ticket.Date,
            ticket.Location,
            ticket.Name);
    }

    /// <summary>
    /// The ticket of the demo: drawn from scratch, so it shows nothing but
    /// the play and the invented reservation, and is marked as not valid.
    /// </summary>
    public async Task DownloadDemoTicketAsync(Ticket ticket, Production production)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync(
            "downloadDemoTicket",
            ticket.FileName,
            production.Company,
            production.Title,
            ticket.Date,
            ticket.Location,
            ticket.Name,
            ticket.Seats);
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/toneel.js");

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is already gone.
        }
    }
}
