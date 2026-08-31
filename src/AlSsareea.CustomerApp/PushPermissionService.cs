namespace AlSsareea.CustomerApp;

public interface IPushPermissionService { Task<bool> RequestAsync(); }

public sealed class PushPermissionService : IPushPermissionService
{
    public async Task<bool> RequestAsync()
    {
#if ANDROID
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted) return true;
        return await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;
#elif IOS
        UserNotifications.UNAuthorizationStatus status = (await UserNotifications.UNUserNotificationCenter.Current.GetNotificationSettingsAsync()).AuthorizationStatus;
        if (status == UserNotifications.UNAuthorizationStatus.Authorized) return true;
        if (status == UserNotifications.UNAuthorizationStatus.Denied) return false;
        (bool granted, _) = await UserNotifications.UNUserNotificationCenter.Current.RequestAuthorizationAsync(UserNotifications.UNAuthorizationOptions.Alert | UserNotifications.UNAuthorizationOptions.Badge | UserNotifications.UNAuthorizationOptions.Sound);
        if (granted) UIKit.UIApplication.SharedApplication.RegisterForRemoteNotifications();
        return granted;
#else
        return false;
#endif
    }
}
