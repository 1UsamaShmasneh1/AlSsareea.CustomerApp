using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Firebase.Messaging;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class AndroidFirebaseMessagingService : FirebaseMessagingService
{
    private const string ChannelId = "orders";

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        if (!string.IsNullOrWhiteSpace(token)) PushTokenBridge.Current?.Publish(token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        Dictionary<string, string> data = message.Data.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (data.Count == 0) return;
        MainThread.BeginInvokeOnMainThread(async () => await AppServices.Get<PushMessageDispatcher>().DispatchAsync(data));
        ShowNotification(message, data);
    }

    private void ShowNotification(RemoteMessage message, IReadOnlyDictionary<string, string> data)
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) manager.CreateNotificationChannel(new NotificationChannel(ChannelId, AppServices.Get<ILocalizationService>()["Orders"], NotificationImportance.Default));
        Intent intent = new(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        foreach (KeyValuePair<string, string> pair in data) intent.PutExtra(pair.Key, pair.Value);
        PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23)) flags |= PendingIntentFlags.Immutable;
        PendingIntent pending = PendingIntent.GetActivity(this, data.GetHashCode(), intent, flags)!;
        string title = message.GetNotification()?.Title ?? AppServices.Get<ILocalizationService>()["AppName"];
        string body = message.GetNotification()?.Body ?? string.Empty;
        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle(title);
        builder.SetContentText(body);
        builder.SetAutoCancel(true);
        builder.SetContentIntent(pending);
        Notification? notification = builder.Build();
        if (notification is null) return;
        manager.Notify(message.MessageId?.GetHashCode() ?? Environment.TickCount, notification);
    }
}
