using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        DispatchPushIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        DispatchPushIntent(intent);
    }

    private static void DispatchPushIntent(Intent? intent)
    {
        if (intent?.Extras is null) return;
        Dictionary<string, string> data = intent.Extras.KeySet()!
            .Select(key => new KeyValuePair<string, string>(key, intent.Extras.GetString(key) ?? string.Empty))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (data.Count == 0) return;
        MainThread.BeginInvokeOnMainThread(async () => await AppServices.Get<PushMessageDispatcher>().DispatchAsync(data));
    }
}
