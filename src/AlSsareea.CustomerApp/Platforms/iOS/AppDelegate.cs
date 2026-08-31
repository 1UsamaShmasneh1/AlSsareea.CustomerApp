using Foundation;

namespace AlSsareea.CustomerApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIKit.UIApplication application, NSData deviceToken)
    {
        string token = Convert.ToHexString(deviceToken.ToArray()).ToLowerInvariant();
        PushTokenBridge.Current?.Publish(token);
    }
}
