# Platform setup and validation

## Android

The Android emulator reaches the Windows backend through `https://10.0.2.2:7080/`. Development HTTPS trust must be configured on the emulator or a local-only HTTP endpoint must be selected explicitly. Internet, network-state, and Android 13 notification permissions are declared.

MAUI Maps requires a Google Maps Android client key supplied through deployment configuration. FCM requires a real Firebase client project, `google-services.json`, and the deployment-selected compatible Firebase Messaging binding. These files are intentionally absent and ignored. The app's `PushTokenBridge.Publish` hook accepts the resulting rotated FCM token and the shared coordinator performs backend registration.

## iOS

iOS compilation can be validated on Windows, but runtime requires a paired Mac with Xcode and Apple signing. `AppDelegate.RegisteredForRemoteNotifications` captures the APNs token and publishes it to the shared registration coordinator. Notification authorization is requested only from contextual UI. APNs provider credentials never belong in the app.

## Deep links and notification navigation

Supported URIs are `alssareea://orders/{guid}`, `alssareea://tracking/{guid}`, and `alssareea://notifications/{guid}`. Parsing rejects other schemes, routes, and malformed IDs. Navigation occurs only for an authenticated session; the backend still performs authorization.

## External runtime limitations

No emulator/device, Firebase project, APNs signing environment, or production map key is included in source. Compile/code integration can be verified without those external assets; real provider registration, push delivery, and Android map tile rendering cannot.
