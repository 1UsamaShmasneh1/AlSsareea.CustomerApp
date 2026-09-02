using Android.App;
using Android.Content;
using Microsoft.Maui.Authentication;

namespace AlSsareea.CustomerApp;

[Activity(NoHistory = true, Exported = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "alssareea", DataHost = "oauth2redirect")]
public sealed class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity;
