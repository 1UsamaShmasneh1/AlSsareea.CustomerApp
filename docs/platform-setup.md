# Platform setup and validation

## Android

The Android emulator reaches the Backend `http` launch profile through `http://10.0.2.2:5257/`. Cleartext traffic is permitted only in Debug; non-Debug Android manifests explicitly disable it. Windows uses `http://localhost:5257/`. Override `ALSSAREEA_API_BASE_URL` for a physical device or a different development host. Internet, network-state, and Android 13 notification permissions are declared.

MAUI Maps requires a Google Maps Android API key supplied through `ALSSAREEA_GOOGLE_MAPS_API_KEY` before the Android build. The build places that public key in the standard `com.google.android.geo.API_KEY` manifest metadata and enables the native map handler. When it is absent, Android does not initialize the native Maps SDK and tracking retains its coordinate-only fallback instead of terminating startup. FCM uses Microsoft's maintained `Xamarin.Firebase.Messaging` 124.1.2 binding. Register Android package `com.alssareea.customer` in Firebase, download its configuration to `src/AlSsareea.CustomerApp/Platforms/Android/google-services.json`, and rebuild. The file is intentionally absent and ignored; when present, the build enables token acquisition. The native service handles token refresh and data messages, while shared code performs replacement-first backend registration and safe navigation.

Google authentication uses `WebAuthenticator` with the system browser, authorization-code flow, PKCE S256, random state, and nonce. In Google Cloud, create an **Android** OAuth client for package `com.alssareea.customer` and the SHA-1 of the APK signing certificate. Because this implementation intentionally retains a custom-scheme browser callback, enable **Custom URI scheme** in that Android client's Advanced Settings. The exact callback is `com.alssareea.customer:/oauth2redirect`; the exported Android callback activity owns that scheme and path. An Android client has no separate Authorized redirect URIs list, and no web client or client secret belongs in the app.

Before building Android, set `ALSSAREEA_GOOGLE_CLIENT_ID` to that public Android client ID. `ALSSAREEA_GOOGLE_REDIRECT_URI` is optional and defaults to the callback above. MSBuild embeds these public values into the application assembly, so rebuild and redeploy the signed APK after changing them. Runtime environment values remain useful for platforms that propagate them and take precedence over embedded build values. Configure the backend with the same client ID in `Authentication__Google__AllowedClientIds__0` and set `Authentication__Google__Enabled=true`.

For a local debug build, obtain the certificate fingerprints from the actual signed APK with Android SDK `apksigner verify --print-certs <signed-apk>`, or from the Xamarin/MAUI debug keystore with JDK `keytool -list -v -keystore <debug.keystore> -alias androiddebugkey -storepass android -keypass android`. Recalculate them if the signing keystore changes; never expose or commit the keystore or its private key.

## iOS

iOS compilation can be validated on Windows, but runtime requires a paired Mac with Xcode and Apple signing. `AppDelegate.RegisteredForRemoteNotifications` captures the APNs token and publishes it to the shared registration coordinator. Notification authorization is requested only from contextual UI. APNs provider credentials never belong in the app.

The iOS and Mac Catalyst URL-type configuration registers `com.alssareea.customer`, matching `com.alssareea.customer:/oauth2redirect`. Each production platform should use its own platform-appropriate OAuth client; this phase's verified runtime target is Android. Runtime still requires a deployment-owned client identifier and supported signing/device environment. Windows returns a localized unsupported result rather than opening an embedded login view.

## Deep links and notification navigation

Supported URIs are `alssareea://orders/{guid}`, `alssareea://tracking/{guid}`, and `alssareea://notifications/{guid}`. Parsing rejects other schemes, routes, and malformed IDs. Navigation occurs only for an authenticated session; the backend still performs authorization.

## External runtime limitations

No emulator/device, Google OAuth project/client identifier, Firebase project, APNs signing environment, or production map key is included in source. Compile/code integration and fake Google-provider flows can be verified without those external assets; real Google consent/login, provider registration, push delivery, and Android map tile rendering cannot. Email/password registration is independent of this external configuration.
