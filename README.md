# AlSsareea Customer App

.NET 10 MAUI customer client for Android and iOS, with Windows and Mac Catalyst compile targets and a platform-independent Core project.

## Architecture

`AlSsareea.CustomerApp` owns MAUI pages, Shell, platform adapters, SecureStorage, connectivity, native maps, push-token hooks, and SignalR transport. `AlSsareea.CustomerApp.Core` owns contracts, typed clients, session rotation, state models, ViewModels, centralized routes, deep links, tracking coordination, idempotency, and localized error/status mapping. Unit tests reference Core only. The app has no backend assembly reference or database access.

The dependency flow is Page → ViewModel → typed API interface → `ApiClient` → backend. Remote screens use `Initial`, `Loading`, `Content`, `Empty`, `Offline`, `Error`, and `Refreshing`. Reads receive one conservative retry for transport or 502/503/504 failures; writes never receive a generic retry.

## Screen map

Splash → onboarding or session restoration → login → five-tab Shell (Home, Search, Cart, Orders, Profile). Detail routes cover merchant details, catalog, product, addresses, checkout, order details, tracking, notifications, and legal information. Route names and deep-link parsing are centralized in Core.

## Local setup

Install SDK 10.0.400 and the `android`, `ios`, `maccatalyst`, and `maui-windows` workloads. The default backend URL is `https://10.0.2.2:7080/` on the Android emulator and `https://localhost:7080/` elsewhere. Override it locally with `ALSSAREEA_API_BASE_URL`; do not commit production URLs or credentials.

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

The shared coordinator registers tokens with Android=1/FCM=1 or iOS=2/APNs=2, registers rotated replacements first, then removes the prior backend device-token record. iOS captures APNs device tokens natively. Android provides the token bridge expected from the deployment's Firebase Messaging integration, but remains disabled until a real `google-services.json` and compatible FCM package/configuration are supplied. Provider files and signing material are ignored. Push permission is requested contextually, not at first frame.

## Tracking and resilience

Tracking loads the REST snapshot, connects to `/hubs/tracking` using only the active access token, invokes `SubscribeOrder`, and listens for `LocationUpdated`. Automatic reconnect reloads REST state and re-subscribes because SignalR groups do not survive reconnection. Connectivity is abstracted for testable offline UI.

## Known backend/deferred scope

The public Catalog product response does not expose variant/option definitions or product media references. The app therefore uses the authoritative price endpoint for the selectable empty option set and does not fabricate option or image data. Completing rich option selection/media requires an explicit customer-safe backend contract. Electronic payment is Phase 22. Support is Phase 24.

Phase 18B is not declared complete in this revision. In addition to that backend contract blocker, remaining client work includes moving every page-level literal into the English/Arabic/Hebrew resources, adding address editing and complete order item/timeline presentation, wiring a concrete Android Firebase Messaging receiver once the deployment selects a compatible binding, and completing the remaining ViewModel/HTTP test cases. The current resource parity test proves only that the resource files agree; it does not claim that every visible page literal has been externalized.
