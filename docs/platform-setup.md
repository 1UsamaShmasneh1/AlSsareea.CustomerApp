# Platform setup and validation

## Android

The Android emulator reaches the Backend `http` launch profile through `http://10.0.2.2:5257/`. Cleartext traffic is permitted only in Debug; non-Debug Android manifests explicitly disable it. Windows uses `http://localhost:5257/`. Override `ALSSAREEA_API_BASE_URL` for a physical device or a different development host. Internet, network-state, and Android 13 notification permissions are declared.

MAUI Maps requires a Google Maps Android client key supplied through deployment configuration. FCM uses Microsoft's maintained `Xamarin.Firebase.Messaging` 124.1.2 binding. Register Android package `com.alssareea.customer` in Firebase, download its configuration to `src/AlSsareea.CustomerApp/Platforms/Android/google-services.json`, and rebuild. The file is intentionally absent and ignored; when present, the build enables token acquisition. The native service handles token refresh and data messages, while shared code performs replacement-first backend registration and safe navigation.

Google authentication uses `WebAuthenticator` with the system browser, authorization-code flow, PKCE S256, random state, and nonce. Register `alssareea://oauth2redirect` for the Android client/application and set `ALSSAREEA_GOOGLE_CLIENT_ID`. The Android callback activity owns that exact scheme and host. No client secret belongs in the app.

## iOS

iOS compilation can be validated on Windows, but runtime requires a paired Mac with Xcode and Apple signing. `AppDelegate.RegisteredForRemoteNotifications` captures the APNs token and publishes it to the shared registration coordinator. Notification authorization is requested only from contextual UI. APNs provider credentials never belong in the app.

The iOS and Mac Catalyst URL-type configuration also registers `alssareea`; the Google OAuth client must allow the matching `alssareea://oauth2redirect` callback. Runtime still requires a deployment-owned client identifier and supported signing/device environment. Windows returns a clean unsupported result rather than opening an embedded login view.

## Deep links and notification navigation

Supported URIs are `alssareea://orders/{guid}`, `alssareea://tracking/{guid}`, and `alssareea://notifications/{guid}`. Parsing rejects other schemes, routes, and malformed IDs. Navigation occurs only for an authenticated session; the backend still performs authorization.

## External runtime limitations

No emulator/device, Google OAuth project/client identifier, Firebase project, APNs signing environment, or production map key is included in source. Compile/code integration and fake Google-provider flows can be verified without those external assets; real Google consent/login, provider registration, push delivery, and Android map tile rendering cannot. Email/password registration is independent of this external configuration.
