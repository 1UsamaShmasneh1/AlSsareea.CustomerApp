using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AlSsareea.CustomerApp.Core;

public enum RemoteStateKind { Initial, Loading, Content, Empty, Offline, Error, Refreshing }
public enum ConnectionState { Disconnected, Connecting, Connected, Reconnecting, Failed }

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }
    protected void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ObservableObject, ICommand
{
    private bool running;
    public bool IsRunning { get => running; private set { if (Set(ref running, value)) CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !IsRunning && (canExecute?.Invoke() ?? true);
    public async void Execute(object? parameter) => await ExecuteAsync();
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null)) return;
        IsRunning = true;
        try { await execute(); }
        finally { IsRunning = false; }
    }
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public static class AppRoutes
{
    public const string Splash = "splash";
    public const string Onboarding = "onboarding";
    public const string Login = "login";
    public const string Otp = "otp";
    public const string RegisterChoice = "register-choice";
    public const string RegisterEmail = "register-email";
    public const string CompleteProfile = "complete-profile";
    public const string Home = "//main/home";
    public const string Search = "//main/search";
    public const string Cart = "//main/cart";
    public const string Orders = "//main/orders";
    public const string Profile = "//main/profile";
    public const string MerchantDetails = "merchant-details";
    public const string Catalog = "catalog";
    public const string ProductDetails = "product-details";
    public const string Addresses = "addresses";
    public const string Checkout = "checkout";
    public const string OrderDetails = "order-details";
    public const string Tracking = "tracking";
    public const string Notifications = "notifications";
    public const string Legal = "legal";
}

public interface INavigationService
{
    Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null);
}

public interface IPreferencesStore
{
    bool OnboardingCompleted { get; set; }
    string Language { get; set; }
}

public interface ILocalizationService
{
    string Language { get; }
    bool IsRightToLeft { get; }
    void Apply(string language);
    string this[string key] { get; }
}

public interface IConnectivityService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

public interface IUserStateResetter { Task ResetAsync(CancellationToken ct); }

public interface IClientRuntimeEnvironment { bool IsDevelopment { get; } }

public sealed record ClientRuntimeEnvironment(bool IsDevelopment) : IClientRuntimeEnvironment;

public sealed class UserStateResetter(IEnumerable<IUserStateResetter> resetters)
{
    public async Task ResetAsync(CancellationToken ct)
    {
        foreach (IUserStateResetter resetter in resetters) await resetter.ResetAsync(ct);
    }
}

public static class UiErrorMapper
{
    public static string Map(Exception exception, ILocalizationService text, bool online) => exception switch
    {
        ApiException { Problem.Code: "auth.otp_resend_blocked" } => text["ErrorOtpResendBlocked"],
        ApiException { Problem.Code: "auth.email_already_registered" } => text["ErrorEmailAlreadyRegistered"],
        ApiException { Problem.Code: "auth.external_link_required" } => text["ErrorExternalLinkRequired"],
        ApiException { Problem.Code: "auth.external_token_invalid" } => text["ErrorGoogleInvalid"],
        ApiException { Problem.Code: "auth.external_provider_unavailable" } => text["ErrorGoogleUnavailable"],
        ApiException { Problem.Status: 429 } => text["ErrorRateLimit"],
        ApiException { Problem.Status: 401 } => text["ErrorUnauthorized"],
        ApiException { Problem.Status: 403 } => text["ErrorForbidden"],
        ApiException { Problem.Status: 404 } => text["ErrorNotFound"],
        ApiException { Problem.Status: 400 or 422 } => text["ErrorValidation"],
        ApiException { Problem.Status: 410 } => text["ErrorOtpExpired"],
        ApiException { Problem.Status: 409 } => text["ErrorConflict"],
        ApiException { Problem.Status: >= 500 } => text["ErrorUnavailable"],
        ApiTimeoutException => text["ErrorTimeout"],
        ApiNetworkException when !online => text["ErrorOffline"],
        ApiNetworkException => text["ErrorNetwork"],
        _ => text["ErrorGeneric"]
    };
}

public abstract class RemoteViewModel : ObservableObject
{
    private RemoteStateKind state;
    private string? errorMessage;
    protected RemoteViewModel(IConnectivityService connectivity, ILocalizationService text)
    {
        Connectivity = connectivity;
        Text = text;
    }
    protected IConnectivityService Connectivity { get; }
    protected ILocalizationService Text { get; }
    public RemoteStateKind State { get => state; protected set { if (Set(ref state, value)) { Raise(nameof(IsBusy)); Raise(nameof(HasError)); Raise(nameof(IsEmpty)); } } }
    public string? ErrorMessage { get => errorMessage; protected set => Set(ref errorMessage, value); }
    public bool IsBusy => State is RemoteStateKind.Loading or RemoteStateKind.Refreshing;
    public bool HasError => State is RemoteStateKind.Error or RemoteStateKind.Offline;
    public bool IsEmpty => State == RemoteStateKind.Empty;
    protected async Task RunAsync(Func<Task> action, bool refreshing = false)
    {
        if (!Connectivity.IsOnline) { State = RemoteStateKind.Offline; ErrorMessage = Text["ErrorOffline"]; return; }
        State = refreshing ? RemoteStateKind.Refreshing : RemoteStateKind.Loading;
        ErrorMessage = null;
        try { await action(); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { State = Connectivity.IsOnline ? RemoteStateKind.Error : RemoteStateKind.Offline; ErrorMessage = UiErrorMapper.Map(exception, Text, Connectivity.IsOnline); }
    }
}

public sealed class IdempotentSubmission
{
    private string? key;
    public string CurrentKey => key ??= Guid.NewGuid().ToString("N");
    public void Complete() => key = null;
}

public static class OrderStatusPresentation
{
    public static string Key(short status) => status switch
    {
        1 => "OrderDraft",
        2 => "OrderPendingPayment",
        3 => "OrderPaymentAuthorized",
        4 => "OrderSubmitted",
        5 => "OrderAccepted",
        6 => "OrderRejected",
        7 => "OrderPreparing",
        8 => "OrderReady",
        9 => "OrderSearchingDriver",
        10 => "OrderDriverAssigned",
        11 => "OrderDriverArriving",
        12 => "OrderPickedUp",
        13 => "OrderInTransit",
        14 => "OrderArrived",
        15 => "OrderDelivered",
        16 => "OrderCancelled",
        17 => "OrderRefundPending",
        18 => "OrderRefunded",
        19 => "OrderFailed",
        _ => "OrderStatusUnknown"
    };
}

public static class OrderCapabilities
{
    public static bool CanCancel(short status) => status is 2 or 3 or 4 or 5 or 7 or 8 or 9 or 10 or 11;
    public static bool CanTrack(short status) => status is 12 or 13 or 14;
}

public sealed record DeepLinkDestination(string Route, Guid Id);
public static class DeepLinkParser
{
    public static DeepLinkDestination? Parse(Uri uri)
    {
        if (!uri.Scheme.Equals("alssareea", StringComparison.OrdinalIgnoreCase)) return null;
        string target = uri.Host.ToLowerInvariant();
        string segment = uri.AbsolutePath.Trim('/');
        if (!Guid.TryParse(segment, out Guid id)) return null;
        return target switch
        {
            "orders" => new(AppRoutes.OrderDetails, id),
            "tracking" => new(AppRoutes.Tracking, id),
            "notifications" => new(AppRoutes.Notifications, id),
            _ => null
        };
    }
}

public static class PushPayloadParser
{
    public static DeepLinkDestination? Parse(IReadOnlyDictionary<string, string> data)
    {
        if (TryValue(data, "deepLink", out string? deepLink) && Uri.TryCreate(deepLink, UriKind.Absolute, out Uri? uri)) return DeepLinkParser.Parse(uri);
        if (!TryValue(data, "destination", out string? destination)) TryValue(data, "type", out destination);
        string idKey = string.Equals(destination, "notifications", StringComparison.OrdinalIgnoreCase) ? "notificationId" : "orderId";
        if (!TryValue(data, idKey, out string? rawId) || !Guid.TryParse(rawId, out Guid id)) return null;
        return destination?.ToLowerInvariant() switch
        {
            "order" or "orders" or "order-details" => new(AppRoutes.OrderDetails, id),
            "tracking" => new(AppRoutes.Tracking, id),
            "notification" or "notifications" => new(AppRoutes.Notifications, id),
            _ => null
        };
    }

    private static bool TryValue(IReadOnlyDictionary<string, string> data, string key, out string? value)
    {
        KeyValuePair<string, string> item = data.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase));
        value = item.Value;
        return !string.IsNullOrWhiteSpace(value);
    }
}

public sealed class PushMessageDispatcher(ISessionManager session, INavigationService navigation)
{
    public async Task<bool> DispatchAsync(IReadOnlyDictionary<string, string> data)
    {
        DeepLinkDestination? destination = PushPayloadParser.Parse(data);
        if (destination is null || !session.IsAuthenticated) return false;
        string key = destination.Route == AppRoutes.Notifications ? "notificationId" : "orderId";
        await navigation.GoToAsync(destination.Route, new Dictionary<string, object> { [key] = destination.Id });
        return true;
    }
}
