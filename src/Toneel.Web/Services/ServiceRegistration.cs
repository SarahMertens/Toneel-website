using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Toneel.Core.Firebase;
using Toneel.Core.Reservations;
using Toneel.Core.Schedule;

namespace Toneel.Web.Services;

public static class ServiceRegistration
{
    /// <summary>Registers everything the app needs. This is the one place that wires classes together.</summary>
    public static IServiceCollection AddToneel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Production.RareWereld);

        // Read from wwwroot/appsettings.json. See FirebaseOptions for why these may be public.
        services.AddSingleton(new FirebaseOptions(
            ApiKey: configuration["Firebase:ApiKey"] ?? "",
            ProjectId: configuration["Firebase:ProjectId"] ?? ""));

        services.AddScoped(_ => new HttpClient());
        services.AddScoped<FirebaseAuthClient>();

        services.AddScoped<BrowserStorage>();
        services.AddScoped<BrowserFiles>();
        services.AddScoped<AuthState>();
        services.AddScoped<Flash>();

        services.AddScoped<DemoReservationStore>();
        services.AddScoped(provider => new FirestoreReservationStore(
            provider.GetRequiredService<HttpClient>(),
            provider.GetRequiredService<FirebaseOptions>(),
            provider.GetRequiredService<AuthState>().GetIdTokenAsync));

        services.AddScoped<IReservationStore, SessionReservationStore>();
        services.AddScoped<ReservationService>();

        return services;
    }
}
