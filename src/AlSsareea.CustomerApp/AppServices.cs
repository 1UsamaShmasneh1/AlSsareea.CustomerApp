using System.Globalization;
using System.Reflection;
using System.Resources;
using AlSsareea.CustomerApp.Core;
using Microsoft.AspNetCore.SignalR.Client;

namespace AlSsareea.CustomerApp;

public static class AppServices
{
    public static IServiceProvider Provider { get; set; } = null!;
    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}

public sealed class MauiNavigationService : INavigationService
{
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null) =>
        parameters is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, new Dictionary<string, object>(parameters));
}

public sealed class MauiPreferencesStore : IPreferencesStore
{
    private const string OnboardingKey = "onboarding.completed.v1";
    private const string LanguageKey = "language.v1";
    public bool OnboardingCompleted { get => Preferences.Default.Get(OnboardingKey, false); set => Preferences.Default.Set(OnboardingKey, value); }
    public string Language { get => Preferences.Default.Get(LanguageKey, "en"); set => Preferences.Default.Set(LanguageKey, Normalize(value)); }
    private static string Normalize(string value) => value is "ar" or "he" ? value : "en";
}

public sealed class MauiLocalizationService : ILocalizationService
{
    private static readonly ResourceManager Resources = new("AlSsareea.CustomerApp.Resources.Strings.AppResources", Assembly.GetExecutingAssembly());
    public string Language { get; private set; } = "en";
    public bool IsRightToLeft => Language is "ar" or "he";
    public string this[string key] => Resources.GetString(key, new CultureInfo(Language)) ?? key;
    public void Apply(string language)
    {
        Language = language is "ar" or "he" ? language : "en";
        CultureInfo culture = new(Language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        if (Application.Current is not null) Application.Current.UserAppTheme = AppTheme.Unspecified;
        if (Shell.Current is not null) Shell.Current.FlowDirection = IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
}

public sealed class MauiConnectivityService : IConnectivityService
{
    public MauiConnectivityService() => Connectivity.Current.ConnectivityChanged += Changed;
    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public event EventHandler<bool>? ConnectivityChanged;
    private void Changed(object? sender, ConnectivityChangedEventArgs args) => ConnectivityChanged?.Invoke(this, args.NetworkAccess == NetworkAccess.Internet);
}

public sealed class SignalRTrackingHubClient(ApiConfiguration configuration, ISessionManager session) : ITrackingHubClient
{
    private HubConnection? connection;
    public ConnectionState State { get; private set; }
    public event EventHandler<TrackingRealtimePayload>? LocationUpdated;
    public event EventHandler<ConnectionState>? StateChanged;
    public event EventHandler? Reconnected;
    public async Task StartAsync(CancellationToken ct)
    {
        if (connection is not null && connection.State != HubConnectionState.Disconnected) return;
        SetState(ConnectionState.Connecting);
        connection = new HubConnectionBuilder()
            .WithUrl(new Uri(configuration.BaseUri, "hubs/tracking"), options => options.AccessTokenProvider = () => Task.FromResult(session.AccessToken))
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)])
            .Build();
        connection.On<TrackingRealtimePayload>("LocationUpdated", value => LocationUpdated?.Invoke(this, value));
        connection.Reconnecting += _ => { SetState(ConnectionState.Reconnecting); return Task.CompletedTask; };
        connection.Reconnected += _ => { SetState(ConnectionState.Connected); Reconnected?.Invoke(this, EventArgs.Empty); return Task.CompletedTask; };
        connection.Closed += _ => { SetState(ConnectionState.Disconnected); return Task.CompletedTask; };
        try { await connection.StartAsync(ct); SetState(ConnectionState.Connected); }
        catch { SetState(ConnectionState.Failed); throw; }
    }
    public Task SubscribeOrderAsync(Guid orderId, CancellationToken ct) => connection?.InvokeAsync("SubscribeOrder", orderId, ct) ?? throw new InvalidOperationException("Tracking connection is not started.");
    public async Task StopAsync(CancellationToken ct) { if (connection is not null) await connection.StopAsync(ct); SetState(ConnectionState.Disconnected); }
    private void SetState(ConnectionState value) { State = value; StateChanged?.Invoke(this, value); }
    public async ValueTask DisposeAsync() { if (connection is not null) await connection.DisposeAsync(); }
}

public sealed class PushTokenBridge : IPushTokenSource
{
    public static PushTokenBridge? Current { get; private set; }
    private string? token;
    public PushTokenBridge() => Current = this;
    public short Platform => OperatingSystem.IsAndroid() ? PushValues.Android : PushValues.Ios;
    public short Provider => OperatingSystem.IsAndroid() ? PushValues.Fcm : PushValues.Apns;
    public bool IsConfigured => OperatingSystem.IsIOS() || OperatingSystem.IsAndroid() && AndroidFirebaseConfigured;
    public static bool AndroidFirebaseConfigured { get; set; }
    public event EventHandler<string>? TokenChanged;
    public Task<string?> GetTokenAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(token); }
    public void Publish(string value) { token = value; TokenChanged?.Invoke(this, value); }
}
