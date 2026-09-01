using AlSsareea.CustomerApp.Core;
using Microsoft.Extensions.Logging;

namespace AlSsareea.CustomerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if ANDROID || IOS || MACCATALYST
        builder.UseMauiMaps();
#endif
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        builder.Services.AddSingleton(new ApiConfiguration(ResolveBaseUri()));
#if DEBUG
        builder.Services.AddSingleton<IClientRuntimeEnvironment>(new ClientRuntimeEnvironment(true));
#else
        builder.Services.AddSingleton<IClientRuntimeEnvironment>(new ClientRuntimeEnvironment(false));
#endif
        builder.Services.AddSingleton<ISessionStorage, SecureSessionStorage>();
        builder.Services.AddSingleton<IAuthenticationApi>(sp =>
        {
            HttpClient http = new() { BaseAddress = sp.GetRequiredService<ApiConfiguration>().BaseUri, Timeout = TimeSpan.FromSeconds(30) };
            return new AuthenticationApi(new ApiClient(http));
        });
        builder.Services.AddSingleton<ISessionManager, SessionManager>();
        builder.Services.AddSingleton(sp =>
        {
            var authentication = new AuthenticatedHandler(sp.GetRequiredService<ISessionManager>()) { InnerHandler = new HttpClientHandler() };
            var retry = new SafeReadRetryHandler { InnerHandler = authentication };
            return new ApiClient(new HttpClient(retry) { BaseAddress = sp.GetRequiredService<ApiConfiguration>().BaseUri, Timeout = TimeSpan.FromSeconds(30) });
        });

        builder.Services.AddSingleton<ICustomerApi, CustomerApi>();
        builder.Services.AddSingleton<IMerchantApi, MerchantApi>();
        builder.Services.AddSingleton<ICatalogApi, CatalogApi>();
        builder.Services.AddSingleton<ICartApi, CartApi>();
        builder.Services.AddSingleton<IMapsApi, MapsApi>();
        builder.Services.AddSingleton<IOrdersApi, OrdersApi>();
        builder.Services.AddSingleton<ITrackingApi, TrackingApi>();
        builder.Services.AddSingleton<INotificationsApi, NotificationsApi>();
        builder.Services.AddSingleton<IAccountSessionApi, AccountSessionApi>();

        builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        builder.Services.AddSingleton<IPreferencesStore, MauiPreferencesStore>();
        builder.Services.AddSingleton<ILocalizationService, MauiLocalizationService>();
        builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        builder.Services.AddSingleton<CustomerAppState>();
        builder.Services.AddSingleton<IUserStateResetter>(sp => sp.GetRequiredService<CustomerAppState>());
        builder.Services.AddSingleton<ITrackingHubClient, SignalRTrackingHubClient>();
        builder.Services.AddSingleton<TrackingCoordinator>();
        builder.Services.AddSingleton<IUserStateResetter>(sp => sp.GetRequiredService<TrackingCoordinator>());
        builder.Services.AddSingleton<PushTokenBridge>();
        builder.Services.AddSingleton<IPushTokenSource>(sp => sp.GetRequiredService<PushTokenBridge>());
        builder.Services.AddSingleton<IPushRegistrationStore, PushRegistrationPreferencesStore>();
        builder.Services.AddSingleton<PushRegistrationCoordinator>();
        builder.Services.AddSingleton<IUserStateResetter>(sp => sp.GetRequiredService<PushRegistrationCoordinator>());
        builder.Services.AddSingleton<UserStateResetter>();
        builder.Services.AddSingleton<IPushPermissionService, PushPermissionService>();
        builder.Services.AddSingleton<PushMessageDispatcher>();

        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MerchantDiscoveryViewModel>();
        builder.Services.AddTransient<MerchantDetailsViewModel>();
        builder.Services.AddTransient<CatalogViewModel>();
        builder.Services.AddTransient<ProductViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<AddressesViewModel>();
        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<OrderDetailsViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<TrackingViewModel>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        MauiApp app = builder.Build();
        AppServices.Provider = app.Services;
        _ = app.Services.GetRequiredService<PushTokenBridge>();
        return app;
    }

    private static Uri ResolveBaseUri()
    {
        string? configured = Environment.GetEnvironmentVariable("ALSSAREEA_API_BASE_URL");
        if (Uri.TryCreate(configured, UriKind.Absolute, out Uri? value)) return value;
#if ANDROID
        return new("http://10.0.2.2:5257/");
#else
        return new("http://localhost:5257/");
#endif
    }
}

public sealed record ApiConfiguration(Uri BaseUri);
