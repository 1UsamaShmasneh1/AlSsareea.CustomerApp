using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public partial class App : Application
{
    private readonly AppShell shell;
    public App(AppShell shell) { InitializeComponent(); this.shell = shell; }
    protected override Window CreateWindow(IActivationState? activationState) => new(shell);
    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        DeepLinkDestination? destination = DeepLinkParser.Parse(uri);
        if (destination is null || !AppServices.Get<ISessionManager>().IsAuthenticated) return;
        MainThread.BeginInvokeOnMainThread(async () => await AppServices.Get<INavigationService>().GoToAsync(destination.Route, new Dictionary<string, object> { [destination.Route == AppRoutes.Notifications ? "notificationId" : "orderId"] = destination.Id }));
    }
}
