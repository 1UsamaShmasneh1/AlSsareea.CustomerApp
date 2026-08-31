# AlSsareea Customer App

.NET 10 MAUI customer client for Android and iOS, with Windows and Mac Catalyst compile targets and a platform-independent Core project.

## Architecture

`AlSsareea.CustomerApp` owns MAUI pages, Shell, platform adapters, SecureStorage, connectivity, native maps, push-token hooks, and SignalR transport. `AlSsareea.CustomerApp.Core` owns contracts, typed clients, session rotation, state models, ViewModels, centralized routes, deep links, tracking coordination, idempotency, and localized error/status mapping. Unit tests reference Core only. The app has no backend assembly reference or database access.

The dependency flow is Page → ViewModel → typed API interface → `ApiClient` → backend. Remote screens use `Initial`, `Loading`, `Content`, `Empty`, `Offline`, `Error`, and `Refreshing`. Reads receive one conservative retry for transport or 502/503/504 failures; writes never receive a generic retry.

## Screen map

Splash → onboarding or session restoration → login → five-tab Shell (Home, Search, Cart, Orders, Profile). Detail routes cover merchant details, catalog, product, addresses, checkout, order details, tracking, notifications, and legal information. Route names and deep-link parsing are centralized in Core.

## Local setup

Install SDK 10.0.400 and the `android`, `ios`, `maccatalyst`, and `maui-windows` workloads. The Debug backend URL is `http://10.0.2.2:5257/` on the Android emulator and `http://localhost:5257/` elsewhere, matching the Backend `http` launch profile. Android cleartext traffic is enabled only for Debug and explicitly disabled for non-Debug builds. Override the URL locally with `ALSSAREEA_API_BASE_URL` (a physical Android device normally needs the development machine's reachable LAN URL); do not commit production URLs or credentials.

## Local Development / Visual Studio

1. Start Docker Desktop, open `AlSsareea.Backend.slnx`, set `AlSsareea.Api` as the startup project, select the `http` profile, and ensure the repository's `postgres` Compose service is healthy. A fresh database requires every module's existing EF Core migration to be applied as documented by the Backend repository.
2. Press F5 for the Backend and verify `http://localhost:5257/health/ready` and `http://localhost:5257/api/system/info` return HTTP 200.
3. Open `AlSsareea.CustomerApp.slnx`, set `AlSsareea.CustomerApp` as the startup project, select `Windows Machine`, and press F5.
4. For Android, install the emulator component and a compatible AVD (or connect a physical device), select that target, and press F5. The standard Android emulator uses `10.0.2.2` to reach the Windows host; use `ALSSAREEA_API_BASE_URL` for a physical device.

Firebase and Google Maps credentials are optional external development configuration. `google-services.json` belongs at `src/AlSsareea.CustomerApp/Platforms/Android/google-services.json`; never fabricate or commit it. Without Firebase or Maps credentials, ordinary startup remains available, while the corresponding external capability cannot be runtime-validated.

Run:

```powershell
dotnet restore AlSsareea.CustomerApp.slnx
dotnet build AlSsareea.CustomerApp.slnx --no-restore
dotnet test tests/AlSsareea.CustomerApp.UnitTests/AlSsareea.CustomerApp.UnitTests.csproj --no-build --no-restore
```

## Session and security

Access tokens exist only in memory. Rotating refresh tokens, expiry, and device identifier use MAUI SecureStorage. Startup restores through refresh; permanent rejection clears the session. Concurrent 401 responses share one refresh operation and retry once with the rotated access token. Logout calls the authenticated backend endpoint, clears secure and in-memory state, stops tracking, unregisters the remembered push-token record, and returns to Login. Tokens, OTPs, authorization headers, and card data are never logged or persisted in ordinary Preferences.

## Localization

English, Arabic, and Hebrew resources have automated key-parity validation. Language and onboarding completion are non-sensitive Preferences. Arabic and Hebrew apply RTL; English applies LTR. Order-state presentation is centralized and follows the backend's numeric enum exactly.

## Maps

Geocode, reverse-geocode, and delivery eligibility come from backend Maps contracts. Saved addresses remain owned by Customers. Tracking uses MAUI Maps and still presents safe coordinates if tile rendering is unavailable. Android map tiles require a deployment-supplied Google Maps client key; no key is committed. iOS uses the platform map service.

## Push

The shared coordinator registers tokens with Android=1/FCM=1 or iOS=2/APNs=2, persists only the backend registration ID, registers rotated replacements first, then removes the prior backend record. iOS captures APNs tokens natively. Android uses Microsoft's maintained `Xamarin.Firebase.Messaging` 124.1.2 binding, obtains and refreshes FCM tokens, receives data messages, displays native notifications, and forwards payloads to the centralized authenticated deep-link dispatcher. A real `Platforms/Android/google-services.json` remains deployment-owned and ignored. Push permission is requested contextually, not at first frame.

## Tracking and resilience

Tracking loads the REST snapshot, connects to `/hubs/tracking` using only the active access token, invokes `SubscribeOrder`, and listens for `LocationUpdated`. Automatic reconnect reloads REST state and re-subscribes because SignalR groups do not survive reconnection. Connectivity is abstracted for testable offline UI.

## Phase boundaries

The verified Catalog customer product-details contract now drives ordered media, variants, option groups, availability, stable Cart IDs, and backend-authoritative configured pricing. Electronic payment remains Phase 22 and Support remains Phase 24. Real FCM/APNs delivery and Android map tiles still require deployment credentials and supported devices; no provider credentials are committed.
