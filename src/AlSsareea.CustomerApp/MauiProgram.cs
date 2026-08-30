using AlSsareea.CustomerApp.Core;
using Microsoft.Extensions.Logging;
namespace AlSsareea.CustomerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold"); });
        builder.Services.AddSingleton<ISessionStorage, SecureSessionStorage>();
        builder.Services.AddSingleton(new ApiConfiguration(new Uri(
#if ANDROID
            "https://10.0.2.2:7080/")));
#else
            "https://localhost:7080/")));
#endif
        builder.Services.AddSingleton<IAuthenticationApi>(sp => new AuthenticationApi(new ApiClient(new HttpClient { BaseAddress = sp.GetRequiredService<ApiConfiguration>().BaseUri, Timeout = TimeSpan.FromSeconds(30) })));
        builder.Services.AddSingleton<ISessionManager, SessionManager>();
        builder.Services.AddSingleton(sp => { var handler = new AuthenticatedHandler(sp.GetRequiredService<ISessionManager>()) { InnerHandler = new HttpClientHandler() }; return new ApiClient(new HttpClient(handler) { BaseAddress = sp.GetRequiredService<ApiConfiguration>().BaseUri, Timeout = TimeSpan.FromSeconds(30) }); });
        builder.Services.AddSingleton<CustomerApi>(); builder.Services.AddSingleton<CatalogApi>(); builder.Services.AddSingleton<CartApi>(); builder.Services.AddSingleton<OrdersApi>(); builder.Services.AddSingleton<TrackingApi>(); builder.Services.AddSingleton<NotificationsApi>(); builder.Services.AddSingleton<AppShell>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
public sealed record ApiConfiguration(Uri BaseUri);
