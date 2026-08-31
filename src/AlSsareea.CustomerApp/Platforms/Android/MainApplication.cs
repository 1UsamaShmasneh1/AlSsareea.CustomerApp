using Android.App;
using Android.Runtime;

namespace AlSsareea.CustomerApp;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
#if FIREBASE_CONFIGURED
        PushTokenBridge.AndroidFirebaseConfigured = true;
        PushTokenBridge.AndroidTokenResolver = async ct =>
        {
            ct.ThrowIfCancellationRequested();
            return await Firebase.Messaging.FirebaseMessaging.Instance.GetToken();
        };
#endif
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
